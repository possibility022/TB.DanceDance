import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { ShareDialog } from './share-dialog';
import { SharingService } from '../../core/api/sharing.service';
import { VideosService } from '../../core/api/videos.service';
import { SharedLinkResponse } from '../../core/api/api-models';

function createFixture(overrides: {
  getMySharedLinks?: ReturnType<typeof vi.fn>;
  createSharedLink?: ReturnType<typeof vi.fn>;
  revokeSharedLink?: ReturnType<typeof vi.fn>;
  updateCommentSettings?: ReturnType<typeof vi.fn>;
}) {
  const sharing = {
    getMySharedLinks: overrides.getMySharedLinks ?? vi.fn(() => of({ links: [] })),
    createSharedLink: overrides.createSharedLink ?? vi.fn(() => of({ linkId: 'new' })),
    revokeSharedLink: overrides.revokeSharedLink ?? vi.fn(() => of(void 0)),
  };
  const videos = {
    updateCommentSettings: overrides.updateCommentSettings ?? vi.fn(() => of(void 0)),
  };

  TestBed.configureTestingModule({
    imports: [ShareDialog],
    providers: [
      { provide: SharingService, useValue: sharing },
      { provide: VideosService, useValue: videos },
    ],
  });

  const fixture: ComponentFixture<ShareDialog> = TestBed.createComponent(ShareDialog);
  return { fixture, sharing, videos, component: fixture.componentInstance };
}

function open(fixture: ComponentFixture<ShareDialog>, videoId = 'v1', commentVisibility = 0): void {
  fixture.componentRef.setInput('videoId', videoId);
  fixture.componentRef.setInput('commentVisibility', commentVisibility);
  fixture.componentRef.setInput('open', true);
  fixture.detectChanges();
}

describe('ShareDialog', () => {
  it('loads only this recording’s links when opened', () => {
    const links: SharedLinkResponse[] = [
      { linkId: 'l1', videoId: 'v1' },
      { linkId: 'l2', videoId: 'other' },
    ];
    const { fixture, component, sharing } = createFixture({
      getMySharedLinks: vi.fn(() => of({ links })),
    });

    open(fixture);

    expect(sharing.getMySharedLinks).toHaveBeenCalledTimes(1);
    expect(component.links().map((l) => l.linkId)).toEqual(['l1']);
  });

  it('does not load links while closed', () => {
    const { fixture, sharing } = createFixture({});
    fixture.componentRef.setInput('videoId', 'v1');
    fixture.detectChanges();
    expect(sharing.getMySharedLinks).not.toHaveBeenCalled();
  });

  describe('shareUrl', () => {
    it('uses the server-provided share url when present', () => {
      const { component } = createFixture({});
      expect(component.shareUrl({ shareUrl: 'https://x/y', linkId: 'l1' })).toBe('https://x/y');
    });

    it('falls back to an origin-based url built from the link id', () => {
      const { component } = createFixture({});
      expect(component.shareUrl({ linkId: 'l1' })).toBe(`${window.location.origin}/shared/l1`);
    });
  });

  describe('create', () => {
    it('creates a link from the form then refreshes the list', () => {
      const { fixture, component, sharing } = createFixture({});
      open(fixture);

      component.create();

      expect(sharing.createSharedLink).toHaveBeenCalledWith('v1', {
        expirationDays: 7,
        allowComments: true,
        allowAnonymousComments: false,
      });
      expect(component.creating()).toBe(false);
      expect(sharing.getMySharedLinks).toHaveBeenCalledTimes(2); // open + after create
    });

    it('does nothing when the form is invalid', () => {
      const { fixture, component, sharing } = createFixture({});
      open(fixture);
      component.form.controls.expirationDays.setValue(0); // below min

      component.create();

      expect(sharing.createSharedLink).not.toHaveBeenCalled();
    });
  });

  describe('applyVisibility', () => {
    it('persists a changed visibility and updates the saved value', () => {
      const { fixture, component, videos } = createFixture({});
      open(fixture, 'v1', 0);

      component.selectedVisibility.set(2);
      component.applyVisibility();

      expect(videos.updateCommentSettings).toHaveBeenCalledWith('v1', { commentVisibility: 2 });
      expect(component.savedVisibility()).toBe(2);
      expect(component.updatingVisibility()).toBe(false);
    });

    it('does nothing when the selection equals the saved value', () => {
      const { fixture, component, videos } = createFixture({});
      open(fixture, 'v1', 1);

      component.applyVisibility();

      expect(videos.updateCommentSettings).not.toHaveBeenCalled();
    });
  });

  describe('revoke', () => {
    it('revokes a link then refreshes', () => {
      const { fixture, component, sharing } = createFixture({});
      open(fixture);

      component.revoke({ linkId: 'l1' });

      expect(sharing.revokeSharedLink).toHaveBeenCalledWith('l1');
      expect(sharing.getMySharedLinks).toHaveBeenCalledTimes(2);
    });

    it('ignores a link with no id', () => {
      const { fixture, component, sharing } = createFixture({});
      open(fixture);
      component.revoke({});
      expect(sharing.revokeSharedLink).not.toHaveBeenCalled();
    });
  });

  describe('copy / close', () => {
    it('copies the share url and marks the link as copied', async () => {
      const writeText = vi.fn(() => Promise.resolve());
      Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });

      const { fixture, component } = createFixture({});
      open(fixture);

      component.copy({ linkId: 'l1', shareUrl: 'https://x/y' });
      await new Promise((resolve) => setTimeout(resolve, 0));

      expect(writeText).toHaveBeenCalledWith('https://x/y');
      expect(component.copiedLinkId()).toBe('l1');
    });

    it('close() clears the copied state and emits closed', () => {
      const { fixture, component } = createFixture({});
      open(fixture);
      component.copiedLinkId.set('l1');

      let emitted = false;
      component.closed.subscribe(() => (emitted = true));
      component.close();

      expect(component.copiedLinkId()).toBeNull();
      expect(emitted).toBe(true);
    });
  });
});
