import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AddVideosDialog } from './add-videos-dialog';
import { VideoInformation } from '../../core/api/api-models';

const VIDEOS: VideoInformation[] = [
  { videoId: 'v1', blobId: 'b1', name: 'One', converted: true, isOwner: true },
  { videoId: 'v2', blobId: 'b2', name: 'Two', converted: true, isOwner: true },
];

async function setup(inputs: Record<string, unknown> = {}): Promise<ComponentFixture<AddVideosDialog>> {
  await TestBed.configureTestingModule({
    imports: [AddVideosDialog],
    providers: [provideRouter([])],
  }).compileComponents();

  const fixture = TestBed.createComponent(AddVideosDialog);
  for (const [key, value] of Object.entries(inputs)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('AddVideosDialog', () => {
  it('is hidden by default', async () => {
    const closedEl = (await setup({ videos: VIDEOS })).nativeElement as HTMLElement;
    expect(closedEl.querySelector('.modal')?.classList).not.toContain('is-active');
  });

  it('is shown when open', async () => {
    const openEl = (await setup({ open: true, videos: VIDEOS })).nativeElement as HTMLElement;
    expect(openEl.querySelector('.modal')?.classList).toContain('is-active');
  });

  it('lists the addable videos', async () => {
    const el = (await setup({ open: true, videos: VIDEOS })).nativeElement as HTMLElement;
    expect(el.querySelectorAll('app-video-card')).toHaveLength(2);
  });

  it('shows a per-video badge when one is provided', async () => {
    const el = (
      await setup({ open: true, videos: VIDEOS, badges: new Map([['v2', 'Also in Worlds']]) })
    ).nativeElement as HTMLElement;
    const badges = [...el.querySelectorAll('.video-card__badge')].map((b) => b.textContent?.trim());
    expect(badges).toEqual(['Also in Worlds']);
  });

  it('shows the error notification when set', async () => {
    const el = (await setup({ open: true, videos: VIDEOS, error: 'Could not add it' }))
      .nativeElement as HTMLElement;
    expect(el.textContent).toContain('Could not add it');
  });

  it('emits add when a video card requests it', async () => {
    const fixture = await setup({ open: true, videos: VIDEOS });
    const emitted: VideoInformation[] = [];
    fixture.componentInstance.add.subscribe((v) => emitted.push(v));

    const button = fixture.nativeElement.querySelector('.video-card__add') as HTMLButtonElement;
    button.click();

    expect(emitted).toEqual([VIDEOS[0]]);
  });

  it('emits closed without losing the list (stays controlled by the open input)', async () => {
    const fixture = await setup({ open: true, videos: VIDEOS });
    const closed = vi.fn();
    fixture.componentInstance.closed.subscribe(closed);

    const doneButton = [...fixture.nativeElement.querySelectorAll('button')].find((b: HTMLButtonElement) =>
      b.textContent?.includes('Done'),
    ) as HTMLButtonElement;
    doneButton.click();

    expect(closed).toHaveBeenCalledTimes(1);
  });
});
