import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';

import { VideoPlayer } from './video-player';
import { VideosService } from '../../core/api/videos.service';
import { GroupsService } from '../../core/api/groups.service';
import { EventsService } from '../../core/api/events.service';
import { CommentsService } from '../../core/api/comments.service';
import { AuthService } from '../../core/auth/auth.service';
import { VideoInformation } from '../../core/api/api-models';

const CONVERTED: VideoInformation = {
  videoId: 'vid1',
  blobId: 'blob1',
  name: 'Tango',
  converted: true,
};

function createFixture(opts: {
  video?: VideoInformation | null;
  getVideo?: ReturnType<typeof vi.fn>;
  renameVideo?: ReturnType<typeof vi.fn>;
  getVideosForGroup?: ReturnType<typeof vi.fn>;
  getEventVideos?: ReturnType<typeof vi.fn>;
  groupId?: string;
  eventId?: string;
}) {
  const video = opts.video === undefined ? CONVERTED : opts.video;
  const videos = {
    getVideo: opts.getVideo ?? vi.fn(() => of({ videoInformation: video })),
    streamUrl: vi.fn(() => 'https://api/stream'),
    renameVideo: opts.renameVideo ?? vi.fn(() => of(void 0)),
  };
  const groups = { getVideosForGroup: opts.getVideosForGroup ?? vi.fn(() => of({ videos: [] })) };
  const events = { getEventVideos: opts.getEventVideos ?? vi.fn(() => of({ videos: [] })) };
  const comments = {
    getCommentsForVideo: vi.fn(() => of({ comments: [] })),
    updateComment: vi.fn(() => of(void 0)),
    deleteComment: vi.fn(() => of(void 0)),
    hideComment: vi.fn(() => of(void 0)),
    unhideComment: vi.fn(() => of(void 0)),
    reportComment: vi.fn(() => of(void 0)),
  };
  const auth = { getAccessToken: vi.fn(() => of('token')), isAuthenticated: signal(true) };

  TestBed.configureTestingModule({
    imports: [VideoPlayer],
    providers: [
      provideRouter([]),
      { provide: VideosService, useValue: videos },
      { provide: GroupsService, useValue: groups },
      { provide: EventsService, useValue: events },
      { provide: CommentsService, useValue: comments },
      { provide: AuthService, useValue: auth },
    ],
  });

  const fixture: ComponentFixture<VideoPlayer> = TestBed.createComponent(VideoPlayer);
  fixture.componentRef.setInput('blobId', 'blob1');
  if (opts.groupId) fixture.componentRef.setInput('groupId', opts.groupId);
  if (opts.eventId) fixture.componentRef.setInput('eventId', opts.eventId);
  fixture.detectChanges();
  return { fixture, videos, groups, events, comments, auth, component: fixture.componentInstance };
}

describe('VideoPlayer', () => {
  it('loads a converted recording with a stream url and its comments', () => {
    const { component, videos, comments } = createFixture({});

    expect(videos.getVideo).toHaveBeenCalledWith('blob1');
    expect(component.info()?.name).toBe('Tango');
    expect(videos.streamUrl).toHaveBeenCalledWith('blob1', 'token');
    expect(component.streamUrl()).toBe('https://api/stream');
    expect(comments.getCommentsForVideo).toHaveBeenCalledWith('vid1');
    expect(component.loading()).toBe(false);
  });

  it('does not expose a stream url for an unconverted recording', () => {
    const { component, videos } = createFixture({ video: { videoId: 'vid1', blobId: 'blob1', converted: false } });
    expect(component.streamUrl()).toBeNull();
    expect(videos.streamUrl).not.toHaveBeenCalled();
  });

  it('enters the failed state when the recording cannot be loaded', () => {
    const { component } = createFixture({ getVideo: vi.fn(() => throwError(() => new Error('boom'))) });
    expect(component.failed()).toBe(true);
    expect(component.loading()).toBe(false);
  });

  describe('sibling playlist', () => {
    it('loads group siblings when a group scope is provided', () => {
      const getVideosForGroup = vi.fn(() => of({ videos: [{ videoId: 'a' }, { videoId: 'b' }] }));
      const { component, groups } = createFixture({ groupId: 'g1', getVideosForGroup });
      expect(groups.getVideosForGroup).toHaveBeenCalledWith('g1');
      expect(component.siblings()).toHaveLength(2);
    });

    it('loads event siblings when only an event scope is provided', () => {
      const getEventVideos = vi.fn(() => of({ videos: [{ videoId: 'a' }] }));
      const { component, events } = createFixture({ eventId: 'e1', getEventVideos });
      expect(events.getEventVideos).toHaveBeenCalledWith('e1');
      expect(component.siblings()).toHaveLength(1);
    });

    it('has no siblings without a scope', () => {
      const { component } = createFixture({});
      expect(component.siblings()).toEqual([]);
    });
  });

  describe('rename', () => {
    it('startRename seeds the draft from the current name', () => {
      const { component } = createFixture({});
      component.startRename();
      expect(component.editingName()).toBe(true);
      expect(component.nameDraft()).toBe('Tango');
    });

    it('saveRename persists a trimmed name and updates the header', () => {
      const renameVideo = vi.fn(() => of(void 0));
      const { component, videos } = createFixture({ renameVideo });

      component.startRename();
      component.nameDraft.set('  Slow Waltz  ');
      component.saveRename();

      expect(videos.renameVideo).toHaveBeenCalledWith('vid1', { newName: 'Slow Waltz' });
      expect(component.info()?.name).toBe('Slow Waltz');
      expect(component.editingName()).toBe(false);
      expect(component.renaming()).toBe(false);
    });

    it('saveRename ignores a blank name', () => {
      const { component, videos } = createFixture({});
      component.startRename();
      component.nameDraft.set('   ');
      component.saveRename();
      expect(videos.renameVideo).not.toHaveBeenCalled();
    });

    it('cancelRename closes the editor', () => {
      const { component } = createFixture({});
      component.startRename();
      component.cancelRename();
      expect(component.editingName()).toBe(false);
    });
  });

  describe('comment moderation', () => {
    it('edits a comment then reloads the list', () => {
      const { component, comments } = createFixture({});
      component.onSaveEdit({ commentId: 'c1', content: 'edited' });
      expect(comments.updateComment).toHaveBeenCalledWith('c1', { content: 'edited' });
      expect(comments.getCommentsForVideo).toHaveBeenCalledTimes(2); // load + reload
    });

    it('removes, hides, unhides, and reports comments', () => {
      const { component, comments } = createFixture({});
      component.onRemove('c1');
      component.onHide('c1');
      component.onUnhide('c1');
      component.onReport({ commentId: 'c1', reason: 'spam' });

      expect(comments.deleteComment).toHaveBeenCalledWith('c1');
      expect(comments.hideComment).toHaveBeenCalledWith('c1');
      expect(comments.unhideComment).toHaveBeenCalledWith('c1');
      expect(comments.reportComment).toHaveBeenCalledWith('c1', { reason: 'spam' });
    });
  });
});
