import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { VideoList } from './video-list';
import { VideoInformation } from '../../../core/api/api-models';

const VIDEOS: VideoInformation[] = [
  { videoId: 'v1', blobId: 'b1', name: 'One', converted: true },
  { videoId: 'v2', blobId: 'b2', name: 'Two', converted: true },
];

async function setup(
  inputs: Record<string, unknown>,
): Promise<ComponentFixture<VideoList>> {
  await TestBed.configureTestingModule({
    imports: [VideoList],
    providers: [provideRouter([])],
  }).compileComponents();

  const fixture = TestBed.createComponent(VideoList);
  for (const [key, value] of Object.entries(inputs)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('VideoList', () => {
  it('shows the default empty message when there are no videos', async () => {
    const el = (await setup({ videos: [] })).nativeElement as HTMLElement;
    expect(el.querySelector('app-video-card')).toBeNull();
    expect(el.textContent).toContain('No recordings yet.');
  });

  it('shows a custom empty message', async () => {
    const el = (await setup({ videos: [], emptyMessage: 'Nothing here' })).nativeElement as HTMLElement;
    expect(el.textContent).toContain('Nothing here');
  });

  it('renders one card per video', async () => {
    const el = (await setup({ videos: VIDEOS })).nativeElement as HTMLElement;
    expect(el.querySelectorAll('app-video-card')).toHaveLength(2);
  });

  it('passes the shareable flag down so cards show the Share action', async () => {
    const el = (await setup({ videos: VIDEOS, shareable: true })).nativeElement as HTMLElement;
    expect(el.querySelectorAll('button')).toHaveLength(2);
  });

  it('passes the transferable flag down so owner cards show the Transfer action', async () => {
    const ownerVideos = VIDEOS.map((v) => ({ ...v, isOwner: true }));
    const el = (await setup({ videos: ownerVideos, transferable: true }))
      .nativeElement as HTMLElement;
    expect(el.querySelectorAll('.video-card__transfer')).toHaveLength(2);
  });

  describe('queryParams scope', () => {
    it('uses groupId when a group scope is set', async () => {
      const fixture = await setup({ videos: VIDEOS, scopeGroupId: 'g1' });
      expect(fixture.componentInstance.queryParams()).toEqual({ groupId: 'g1' });
    });

    it('uses eventId when only an event scope is set', async () => {
      const fixture = await setup({ videos: VIDEOS, scopeEventId: 'e1' });
      expect(fixture.componentInstance.queryParams()).toEqual({ eventId: 'e1' });
    });

    it('prefers the group scope when both are set', async () => {
      const fixture = await setup({ videos: VIDEOS, scopeGroupId: 'g1', scopeEventId: 'e1' });
      expect(fixture.componentInstance.queryParams()).toEqual({ groupId: 'g1' });
    });

    it('is empty when no scope is set', async () => {
      const fixture = await setup({ videos: VIDEOS });
      expect(fixture.componentInstance.queryParams()).toEqual({});
    });
  });
});
