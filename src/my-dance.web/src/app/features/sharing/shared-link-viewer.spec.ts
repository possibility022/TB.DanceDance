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
}) {
  const authed: WritableSignal<boolean> = signal(opts.authed ?? false);
  const sharing = {
    getSharedVideo:
      opts.getSharedVideo ?? vi.fn(() => of(opts.info ?? { videoId: 'v1', name: 'Shared' })),
    sharedStreamUrl: vi.fn(() => 'https://api/stream'),
  };
  const comments = {
    getCommentsByLink: vi.fn(() => of({ comments: [] })),
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
    expect(comments.getCommentsByLink).toHaveBeenCalledWith('link-1');
    expect(component.loading()).toBe(false);
  });

  it('enters the failed state when the link is unavailable', () => {
    const { component } = createFixture({ getSharedVideo: vi.fn(() => throwError(() => new Error('gone'))) });
    expect(component.failed()).toBe(true);
    expect(component.loading()).toBe(false);
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
    it('edits a comment then reloads', () => {
      const { component, comments } = createFixture({});
      component.onSaveEdit({ commentId: 'c1', content: 'edited' });
      expect(comments.updateComment).toHaveBeenCalledWith('c1', { content: 'edited' });
      expect(comments.getCommentsByLink).toHaveBeenCalledTimes(2);
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
