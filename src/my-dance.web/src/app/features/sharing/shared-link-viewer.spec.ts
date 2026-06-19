import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WritableSignal, signal } from '@angular/core';
import { of, throwError } from 'rxjs';

import { SharedLinkViewer } from './shared-link-viewer';
import { SharingService } from '../../core/api/sharing.service';
import { CommentsService } from '../../core/api/comments.service';
import { AuthService } from '../../core/auth/auth.service';
import { AnonymousIdService } from '../../core/anonymous-id.service';
import { SharedVideoInfoResponse } from '../../core/api/api-models';

const ANON_ID = 'anon-1';

function createFixture(opts: {
  authed?: boolean;
  info?: SharedVideoInfoResponse;
  getSharedVideo?: ReturnType<typeof vi.fn>;
  addCommentByLink?: ReturnType<typeof vi.fn>;
  getCommentsByLink?: ReturnType<typeof vi.fn>;
}) {
  const authed: WritableSignal<boolean> = signal(opts.authed ?? false);
  const sharing = {
    getSharedVideo:
      opts.getSharedVideo ?? vi.fn(() => of(opts.info ?? { videoId: 'v1', name: 'Shared' })),
    sharedStreamUrl: vi.fn(() => 'https://api/stream'),
    sharedVideoStreamUrl: vi.fn((linkId: string, videoId: string) => `https://api/stream/${videoId}`),
  };
  const comments = {
    getCommentsByLink: opts.getCommentsByLink ?? vi.fn(() => of({ items: [], totalCount: 0 })),
    addCommentByLink: opts.addCommentByLink ?? vi.fn(() => of({ id: 'c1' })),
    updateComment: vi.fn(() => of(void 0)),
    deleteComment: vi.fn(() => of(void 0)),
    hideComment: vi.fn(() => of(void 0)),
    unhideComment: vi.fn(() => of(void 0)),
    reportComment: vi.fn(() => of(void 0)),
  };

  TestBed.configureTestingModule({
    imports: [SharedLinkViewer],
    providers: [
      { provide: SharingService, useValue: sharing },
      { provide: CommentsService, useValue: comments },
      { provide: AuthService, useValue: { isAuthenticated: authed } },
      { provide: AnonymousIdService, useValue: { getId: () => ANON_ID } },
    ],
  });

  const fixture: ComponentFixture<SharedLinkViewer> = TestBed.createComponent(SharedLinkViewer);
  fixture.componentRef.setInput('linkId', 'link-1');
  fixture.detectChanges();
  return { fixture, sharing, comments, authed, component: fixture.componentInstance };
}

describe('SharedLinkViewer', () => {
  it('loads the shared recording, stream url, and comments', () => {
    const { component, sharing, comments } = createFixture({ info: { videoId: 'v1', name: 'Shared' } });

    expect(sharing.getSharedVideo).toHaveBeenCalledWith('link-1');
    expect(component.info()?.name).toBe('Shared');
    expect(component.streamUrl()).toBe('https://api/stream');
    expect(comments.getCommentsByLink).toHaveBeenCalledWith('link-1', 1, 20);
    expect(component.loading()).toBe(false);
  });

  it('enters the failed state when the link is unavailable', () => {
    const { component } = createFixture({ getSharedVideo: vi.fn(() => throwError(() => new Error('gone'))) });
    expect(component.failed()).toBe(true);
    expect(component.loading()).toBe(false);
  });

  it('renders a competition link as multiple videos with per-video stream urls', () => {
    const { component, comments } = createFixture({
      info: {
        name: 'Nationals',
        isCompetition: true,
        allowCommentsOnThisLink: true,
        videos: [
          { videoId: 'v1', name: 'Round 1' },
          { videoId: 'v2', name: 'Round 2' },
        ],
      },
    });

    expect(component.isCompetition()).toBe(true);
    const videos = component.competitionVideos();
    expect(videos).toHaveLength(2);
    expect(videos[0]).toMatchObject({ videoId: 'v1', url: 'https://api/stream/v1' });
    expect(videos[1]).toMatchObject({ videoId: 'v2', url: 'https://api/stream/v2' });
    // Still one combined thread, loaded by link id.
    expect(comments.getCommentsByLink).toHaveBeenCalledWith('link-1', 1, 20);
  });

  it('treats a single-video link as not a competition', () => {
    const { component } = createFixture({ info: { videoId: 'v1', name: 'Shared', isCompetition: false } });
    expect(component.isCompetition()).toBe(false);
    expect(component.competitionVideos()).toHaveLength(0);
  });

  describe('canCompose', () => {
    it('is false when the link disallows comments', () => {
      const { component } = createFixture({ authed: true, info: { allowCommentsOnThisLink: false } });
      expect(component.canCompose()).toBe(false);
    });

    it('is true for a signed-in user when comments are allowed', () => {
      const { component } = createFixture({ authed: true, info: { allowCommentsOnThisLink: true } });
      expect(component.canCompose()).toBe(true);
    });

    it('is false for an anonymous user unless anonymous comments are allowed', () => {
      const { component } = createFixture({
        authed: false,
        info: { allowCommentsOnThisLink: true, allowAnonymousCommentsOnThisLink: false },
      });
      expect(component.canCompose()).toBe(false);
    });

    it('is true for an anonymous user when anonymous comments are allowed', () => {
      const { component } = createFixture({
        authed: false,
        info: { allowCommentsOnThisLink: true, allowAnonymousCommentsOnThisLink: true },
      });
      expect(component.canCompose()).toBe(true);
    });
  });

  it('requires a signature for anonymous users', () => {
    expect(createFixture({ authed: false }).component.requireSignature()).toBe(true);
  });

  it('does not require a signature for signed-in users', () => {
    expect(createFixture({ authed: true }).component.requireSignature()).toBe(false);
  });

  describe('onCreate', () => {
    it('attaches the anonymous id for guests and reloads comments', () => {
      const addCommentByLink = vi.fn(() => of({ id: 'c1' }));
      const { component, comments } = createFixture({ authed: false, addCommentByLink });

      component.onCreate({ content: 'hello', authorName: 'Guest' });

      expect(addCommentByLink).toHaveBeenCalledWith('link-1', {
        content: 'hello',
        authorName: 'Guest',
        anonymousId: ANON_ID,
      });
      expect(component.submitting()).toBe(false);
      expect(comments.getCommentsByLink).toHaveBeenCalledTimes(2); // load + reload
      expect(comments.getCommentsByLink).toHaveBeenLastCalledWith('link-1', 1, 20); // 1 page loaded so far
    });

    it('omits the anonymous id for signed-in users', () => {
      const addCommentByLink = vi.fn(() => of({ id: 'c1' }));
      const { component } = createFixture({ authed: true, addCommentByLink });

      component.onCreate({ content: 'hello' });

      expect(addCommentByLink).toHaveBeenCalledWith('link-1', {
        content: 'hello',
        authorName: undefined,
        anonymousId: undefined,
      });
    });

    it('clears the submitting flag when posting fails', () => {
      const addCommentByLink = vi.fn(() => throwError(() => new Error('x')));
      const { component } = createFixture({ authed: true, addCommentByLink });

      component.onCreate({ content: 'hello' });

      expect(component.submitting()).toBe(false);
    });
  });

  describe('moderation passthrough', () => {
    it('edits a comment then reloads the currently-loaded amount', () => {
      const { component, comments } = createFixture({});
      component.onSaveEdit({ commentId: 'c1', content: 'edited' });
      expect(comments.updateComment).toHaveBeenCalledWith('c1', { content: 'edited' });
      expect(comments.getCommentsByLink).toHaveBeenCalledTimes(2);
      expect(comments.getCommentsByLink).toHaveBeenLastCalledWith('link-1', 1, 20); // 1 page loaded so far
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
      const getCommentsByLink = vi.fn(() => of({ items: [{ id: 'c1' }], totalCount: 3 }));
      const { component } = createFixture({ getCommentsByLink });
      expect(component.canLoadMoreComments()).toBe(true);
    });

    it('hides load more once every comment has been loaded', () => {
      const getCommentsByLink = vi.fn(() => of({ items: [{ id: 'c1' }, { id: 'c2' }], totalCount: 2 }));
      const { component } = createFixture({ getCommentsByLink });
      expect(component.canLoadMoreComments()).toBe(false);
    });

    it('loadMoreComments appends the next page and tracks whether more remain', () => {
      const calls: Array<[string, number, number]> = [];
      const getCommentsByLink = vi.fn((linkId: string, page: number, pageSize: number) => {
        calls.push([linkId, page, pageSize]);
        return page === 1
          ? of({ items: [{ id: 'c1' }], totalCount: 3 })
          : of({ items: [{ id: 'c2' }], totalCount: 3 });
      });

      const { component } = createFixture({ getCommentsByLink });
      expect(component.canLoadMoreComments()).toBe(true);

      component.loadMoreComments();

      expect(component.commentList().map((c) => c.id)).toEqual(['c1', 'c2']);
      expect(component.canLoadMoreComments()).toBe(true);
      expect(component.loadingMoreComments()).toBe(false);
      expect(calls).toEqual([
        ['link-1', 1, 20],
        ['link-1', 2, 20],
      ]);
    });

    it('loadMoreComments is a no-op while already loading or when there is nothing more to load', () => {
      const getCommentsByLink = vi.fn(() => of({ items: [{ id: 'c1' }, { id: 'c2' }], totalCount: 2 }));
      const { component } = createFixture({ getCommentsByLink });

      expect(getCommentsByLink).toHaveBeenCalledTimes(1);
      expect(component.canLoadMoreComments()).toBe(false);

      component.loadMoreComments();

      expect(getCommentsByLink).toHaveBeenCalledTimes(1);
    });

    it('a mutation refetches everything currently shown rather than collapsing back to one page', () => {
      const calls: Array<[string, number, number]> = [];
      const getCommentsByLink = vi.fn((linkId: string, page: number, pageSize: number) => {
        calls.push([linkId, page, pageSize]);
        return page === 1 && pageSize === 20
          ? of({ items: Array.from({ length: 20 }, (_, i) => ({ id: `c${i}` })), totalCount: 50 })
          : of({ items: Array.from({ length: 40 }, (_, i) => ({ id: `c${i}` })), totalCount: 50 });
      });

      const { component } = createFixture({ getCommentsByLink });
      component.loadMoreComments(); // now showing 2 pages (40 items)

      component.onRemove('c1');

      expect(calls.at(-1)).toEqual(['link-1', 1, 40]); // refetch sized to the 2 pages already loaded
      expect(component.commentList()).toHaveLength(40);
    });
  });
});
