import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { ViewportScroller } from '@angular/common';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';

import { VideoPlayer } from './video-player';
import { VideosService } from '../../core/api/videos.service';
import { GroupsService } from '../../core/api/groups.service';
import { EventsService } from '../../core/api/events.service';
import { CompetitionsService } from '../../core/api/competitions.service';
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
  deleteVideo?: ReturnType<typeof vi.fn>;
  getVideosForGroup?: ReturnType<typeof vi.fn>;
  getEventVideos?: ReturnType<typeof vi.fn>;
  getCompetition?: ReturnType<typeof vi.fn>;
  getCommentsForVideo?: ReturnType<typeof vi.fn>;
  groupId?: string;
  eventId?: string;
  competitionId?: string;
}) {
  const video = opts.video === undefined ? CONVERTED : opts.video;
  const videos = {
    getVideo: opts.getVideo ?? vi.fn(() => of({ videoInformation: video })),
    streamUrl: vi.fn(() => 'https://api/stream'),
    renameVideo: opts.renameVideo ?? vi.fn(() => of(void 0)),
    deleteVideo: opts.deleteVideo ?? vi.fn(() => of(void 0)),
  };
  const groups = { getVideosForGroup: opts.getVideosForGroup ?? vi.fn(() => of({ items: [] })) };
  const events = { getEventVideos: opts.getEventVideos ?? vi.fn(() => of({ items: [] })) };
  const competitions = { getCompetition: opts.getCompetition ?? vi.fn(() => of({ videos: [] })) };
  const comments = {
    getCommentsForVideo: opts.getCommentsForVideo ?? vi.fn(() => of({ items: [], totalCount: 0 })),
    updateComment: vi.fn(() => of(void 0)),
    deleteComment: vi.fn(() => of(void 0)),
    hideComment: vi.fn(() => of(void 0)),
    unhideComment: vi.fn(() => of(void 0)),
    reportComment: vi.fn(() => of(void 0)),
  };
  const auth = { getAccessToken: vi.fn(() => of('token')), isAuthenticated: signal(true) };
  const viewport = { scrollToPosition: vi.fn() };

  TestBed.configureTestingModule({
    imports: [VideoPlayer],
    providers: [
      provideRouter([]),
      { provide: VideosService, useValue: videos },
      { provide: GroupsService, useValue: groups },
      { provide: EventsService, useValue: events },
      { provide: CompetitionsService, useValue: competitions },
      { provide: CommentsService, useValue: comments },
      { provide: AuthService, useValue: auth },
      { provide: ViewportScroller, useValue: viewport },
    ],
  });

  const fixture: ComponentFixture<VideoPlayer> = TestBed.createComponent(VideoPlayer);
  fixture.componentRef.setInput('blobId', 'blob1');
  if (opts.groupId) fixture.componentRef.setInput('groupId', opts.groupId);
  if (opts.eventId) fixture.componentRef.setInput('eventId', opts.eventId);
  if (opts.competitionId) fixture.componentRef.setInput('competitionId', opts.competitionId);
  fixture.detectChanges();
  return {
    fixture,
    videos,
    groups,
    events,
    competitions,
    comments,
    auth,
    viewport,
    component: fixture.componentInstance,
  };
}

describe('VideoPlayer', () => {
  it('loads a converted recording with a stream url and its comments', () => {
    const { component, videos, comments } = createFixture({});

    expect(videos.getVideo).toHaveBeenCalledWith('blob1');
    expect(component.info()?.name).toBe('Tango');
    expect(videos.streamUrl).toHaveBeenCalledWith('blob1', 'token');
    expect(component.streamUrl()).toBe('https://api/stream');
    expect(comments.getCommentsForVideo).toHaveBeenCalledWith('vid1', 1, 20);
    expect(component.loading()).toBe(false);
  });

  it('hides the native video download control', () => {
    const { fixture } = createFixture({});
    const video = fixture.nativeElement.querySelector('video') as HTMLVideoElement;

    expect(video.getAttribute('controlsList')).toBe('nodownload');
  });

  it('sets the video poster from the recording thumbnail', () => {
    const { fixture } = createFixture({
      video: { ...CONVERTED, thumbnailUrl: 'https://azurite/thumbnails/abc?sv=2024' },
    });
    const video = fixture.nativeElement.querySelector('video') as HTMLVideoElement;

    expect(video.getAttribute('poster')).toBe('https://azurite/thumbnails/abc?sv=2024');
  });

  it('omits the poster attribute when the recording has no thumbnail', () => {
    const { fixture } = createFixture({});
    const video = fixture.nativeElement.querySelector('video') as HTMLVideoElement;

    expect(video.getAttribute('poster')).toBeNull();
  });

  it('does not expose a stream url for an unconverted recording', () => {
    const { component, videos } = createFixture({
      video: { videoId: 'vid1', blobId: 'blob1', converted: false },
    });
    expect(component.streamUrl()).toBeNull();
    expect(videos.streamUrl).not.toHaveBeenCalled();
  });

  it('enters the failed state when the recording cannot be loaded', () => {
    const { component } = createFixture({
      getVideo: vi.fn(() => throwError(() => new Error('boom'))),
    });
    expect(component.failed()).toBe(true);
    expect(component.loading()).toBe(false);
  });

  describe('sibling playlist', () => {
    it('loads group siblings when a group scope is provided', () => {
      const getVideosForGroup = vi.fn(() => of({ items: [{ videoId: 'a' }, { videoId: 'b' }] }));
      const { component, groups } = createFixture({ groupId: 'g1', getVideosForGroup });
      expect(groups.getVideosForGroup).toHaveBeenCalledWith('g1', 1, 100);
      expect(component.siblings()).toHaveLength(2);
    });

    it('loads event siblings when only an event scope is provided', () => {
      const getEventVideos = vi.fn(() => of({ items: [{ videoId: 'a' }] }));
      const { component, events } = createFixture({ eventId: 'e1', getEventVideos });
      expect(events.getEventVideos).toHaveBeenCalledWith('e1', 1, 100);
      expect(component.siblings()).toHaveLength(1);
    });

    it('loads competition siblings when only a competition scope is provided', () => {
      const getCompetition = vi.fn(() =>
        of({ videos: [{ videoId: 'a' }, { videoId: 'b' }, { videoId: 'c' }] }),
      );
      const { component, competitions } = createFixture({ competitionId: 'c1', getCompetition });
      expect(competitions.getCompetition).toHaveBeenCalledWith('c1');
      expect(component.siblings()).toHaveLength(3);
    });

    it('scrolls back to the top when switching to a sibling recording', () => {
      const { fixture, viewport } = createFixture({
        groupId: 'g1',
        getVideosForGroup: vi.fn(() => of({ items: [{ videoId: 'a' }, { videoId: 'b' }] })),
      });
      // The component is reused across siblings; selecting one swaps the player
      // in place, so it must scroll the viewport back up to the new video.
      viewport.scrollToPosition.mockClear();

      fixture.componentRef.setInput('blobId', 'blob2');
      fixture.detectChanges();

      expect(viewport.scrollToPosition).toHaveBeenCalledWith([0, 0]);
    });

    it('has no siblings without a scope', () => {
      const { component } = createFixture({});
      expect(component.siblings()).toEqual([]);
    });

    it('leaves siblings empty when the sibling request fails', () => {
      const getVideosForGroup = vi.fn(() => throwError(() => new Error('x')));
      const { component } = createFixture({ groupId: 'g1', getVideosForGroup });
      expect(component.siblings()).toEqual([]);
    });

    it('labels the sibling tab for the active group scope', () => {
      expect(createFixture({ groupId: 'g1' }).component.siblingScope()).toBe('group');
    });

    it('labels the sibling tab for the active event scope', () => {
      expect(createFixture({ eventId: 'e1' }).component.siblingScope()).toBe('event');
    });

    it('labels the sibling tab for the active competition scope', () => {
      expect(createFixture({ competitionId: 'c1' }).component.siblingScope()).toBe('competition');
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

    it('saveRename clears the in-flight flag when the request fails', () => {
      const renameVideo = vi.fn(() => throwError(() => new Error('x')));
      const { component } = createFixture({ renameVideo });
      component.startRename();
      component.nameDraft.set('New');
      component.saveRename();
      expect(component.renaming()).toBe(false);
      expect(component.info()?.name).toBe('Tango'); // unchanged on failure
    });

    it('cancelRename closes the editor', () => {
      const { component } = createFixture({});
      component.startRename();
      component.cancelRename();
      expect(component.editingName()).toBe(false);
    });
  });

  describe('delete', () => {
    afterEach(() => vi.restoreAllMocks());

    const deleteButton = (fixture: ComponentFixture<VideoPlayer>) =>
      fixture.nativeElement.querySelector('button[aria-label="Delete recording"]') as
        | HTMLButtonElement
        | null;

    it('shows the Delete button when the user owns the recording', () => {
      const { fixture } = createFixture({ video: { ...CONVERTED, isOwner: true } });
      expect(deleteButton(fixture)).not.toBeNull();
    });

    it('hides the Delete button when the user does not own the recording', () => {
      const { fixture } = createFixture({ video: { ...CONVERTED, isOwner: false } });
      expect(deleteButton(fixture)).toBeNull();
    });

    it('confirms, deletes, and navigates back to the library', () => {
      vi.spyOn(window, 'confirm').mockReturnValue(true);
      const deleteVideo = vi.fn(() => of(void 0));
      const { component } = createFixture({ deleteVideo });
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      component.deleteVideo();

      expect(deleteVideo).toHaveBeenCalledWith('vid1');
      expect(navigate).toHaveBeenCalledWith(['/videos/my']);
      expect(component.deleting()).toBe(false);
    });

    it('is a no-op when the confirmation is dismissed', () => {
      vi.spyOn(window, 'confirm').mockReturnValue(false);
      const deleteVideo = vi.fn(() => of(void 0));
      const { component } = createFixture({ deleteVideo });
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      component.deleteVideo();

      expect(deleteVideo).not.toHaveBeenCalled();
      expect(navigate).not.toHaveBeenCalled();
    });

    it('clears the in-flight flag and stays on the page when the delete fails', () => {
      vi.spyOn(window, 'confirm').mockReturnValue(true);
      const deleteVideo = vi.fn(() => throwError(() => new Error('boom')));
      const { component } = createFixture({ deleteVideo });
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      component.deleteVideo();

      expect(deleteVideo).toHaveBeenCalledWith('vid1');
      expect(navigate).not.toHaveBeenCalled();
      expect(component.deleting()).toBe(false);
    });
  });

  describe('sidebar tabs', () => {
    it('defaults to the recordings tab when siblings are loaded', () => {
      const { component } = createFixture({
        groupId: 'g1',
        getVideosForGroup: vi.fn(() => of({ items: [{ videoId: 'a' }] })),
      });
      expect(component.activeTab()).toBe('recordings');
    });

    it('switches to the comments tab when requested', () => {
      const { component } = createFixture({
        groupId: 'g1',
        getVideosForGroup: vi.fn(() => of({ items: [{ videoId: 'a' }] })),
      });
      component.setTab('comments');
      expect(component.activeTab()).toBe('comments');
    });

    it('falls back to comments when no siblings are loaded', () => {
      const { component } = createFixture({});
      expect(component.siblings()).toHaveLength(0);
      expect(component.activeTab()).toBe('comments');
    });
  });

  describe('comment moderation', () => {
    it('edits a comment then reloads the currently-loaded amount', () => {
      const { component, comments } = createFixture({});
      component.onSaveEdit({ commentId: 'c1', content: 'edited' });
      expect(comments.updateComment).toHaveBeenCalledWith('c1', { content: 'edited' });
      expect(comments.getCommentsForVideo).toHaveBeenCalledTimes(2); // load + reload
      expect(comments.getCommentsForVideo).toHaveBeenLastCalledWith('vid1', 1, 20); // 1 page loaded so far
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

  describe('comment paging', () => {
    it('exposes canLoadMoreComments when there are more comments than the first page returned', () => {
      const getCommentsForVideo = vi.fn(() => of({ items: [{ id: 'c1' }], totalCount: 3 }));
      const { component } = createFixture({ getCommentsForVideo });
      expect(component.canLoadMoreComments()).toBe(true);
    });

    it('hides load more once every comment has been loaded', () => {
      const getCommentsForVideo = vi.fn(() => of({ items: [{ id: 'c1' }, { id: 'c2' }], totalCount: 2 }));
      const { component } = createFixture({ getCommentsForVideo });
      expect(component.canLoadMoreComments()).toBe(false);
    });

    it('loadMoreComments appends the next page and tracks whether more remain', () => {
      const calls: Array<[string, number, number]> = [];
      const getCommentsForVideo = vi.fn((videoId: string, page: number, pageSize: number) => {
        calls.push([videoId, page, pageSize]);
        return page === 1
          ? of({ items: [{ id: 'c1' }], totalCount: 3 })
          : of({ items: [{ id: 'c2' }], totalCount: 3 });
      });

      const { component } = createFixture({ getCommentsForVideo });
      expect(component.canLoadMoreComments()).toBe(true);

      component.loadMoreComments();

      expect(component.commentList().map((c) => c.id)).toEqual(['c1', 'c2']);
      expect(component.canLoadMoreComments()).toBe(true);
      expect(component.loadingMoreComments()).toBe(false);
      expect(calls).toEqual([
        ['vid1', 1, 20],
        ['vid1', 2, 20],
      ]);
    });

    it('loadMoreComments is a no-op while already loading or when there is nothing more to load', () => {
      const getCommentsForVideo = vi.fn(() => of({ items: [{ id: 'c1' }, { id: 'c2' }], totalCount: 2 }));
      const { component } = createFixture({ getCommentsForVideo });

      expect(getCommentsForVideo).toHaveBeenCalledTimes(1);
      expect(component.canLoadMoreComments()).toBe(false);

      component.loadMoreComments();

      expect(getCommentsForVideo).toHaveBeenCalledTimes(1);
    });

    it('a mutation refetches everything currently shown rather than collapsing back to one page', () => {
      const calls: Array<[string, number, number]> = [];
      const getCommentsForVideo = vi.fn((videoId: string, page: number, pageSize: number) => {
        calls.push([videoId, page, pageSize]);
        return page === 1 && pageSize === 20
          ? of({ items: Array.from({ length: 20 }, (_, i) => ({ id: `c${i}` })), totalCount: 50 })
          : of({ items: Array.from({ length: 40 }, (_, i) => ({ id: `c${i}` })), totalCount: 50 });
      });

      const { component } = createFixture({ getCommentsForVideo });
      component.loadMoreComments(); // now showing 2 pages (40 items)

      component.onRemove('c1');

      expect(calls.at(-1)).toEqual(['vid1', 1, 40]); // refetch sized to the 2 pages already loaded
      expect(component.commentList()).toHaveLength(40);
    });
  });
});
