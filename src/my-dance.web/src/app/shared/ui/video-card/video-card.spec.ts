import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { VideoCard } from './video-card';
import { VideoInformation } from '../../../core/api/api-models';

const CONVERTED: VideoInformation = {
  videoId: 'v1',
  blobId: 'blob1',
  name: 'Waltz basics',
  recordedDateTime: new Date(2026, 5, 4),
  duration: '3:21',
  converted: true,
};

async function setup(
  video: VideoInformation,
  inputs: Partial<{
    shareable: boolean;
    selected: boolean;
    queryParams: Record<string, string>;
    badge: string;
  }> = {},
): Promise<ComponentFixture<VideoCard>> {
  const fixture = TestBed.createComponent(VideoCard);
  fixture.componentRef.setInput('video', video);
  for (const [key, value] of Object.entries(inputs)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('VideoCard', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VideoCard],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('renders the name, formatted recorded date, and duration badge', async () => {
    const el = (await setup(CONVERTED)).nativeElement as HTMLElement;
    expect(el.querySelector('h3')?.textContent).toContain('Waltz basics');
    const meta = el.querySelector('p')?.textContent ?? '';
    expect(meta).toContain('04 June 2026');
    expect(meta).not.toContain('3:21');
    expect(el.querySelector('.video-card__duration')?.textContent?.trim()).toBe('3:21');
  });

  it('formats duration once in the preview badge', async () => {
    const el = (await setup({ ...CONVERTED, duration: '00:03:21.0000000' }))
      .nativeElement as HTMLElement;

    expect(el.querySelector('.video-card__duration')?.textContent?.trim()).toBe('3:21');
    expect(el.textContent?.match(/3:21/g)).toHaveLength(1);
  });

  it('formats ISO and seconds durations', async () => {
    const iso = (await setup({ ...CONVERTED, duration: 'PT1H2M3S' })).nativeElement as HTMLElement;
    expect(iso.querySelector('.video-card__duration')?.textContent?.trim()).toBe('1:02:03');

    const seconds = (await setup({ ...CONVERTED, duration: '61' })).nativeElement as HTMLElement;
    expect(seconds.querySelector('.video-card__duration')?.textContent?.trim()).toBe('1:01');
  });

  it('links Watch to the player using the blob id and query params', async () => {
    const el = (await setup(CONVERTED, { queryParams: { groupId: 'g1' } }))
      .nativeElement as HTMLElement;
    const watch = el.querySelector('a.button.is-primary');
    expect(watch?.textContent).toContain('Watch');
    expect(watch?.getAttribute('href')).toBe('/videos/blob1?groupId=g1');
  });

  it('shows a processing tag instead of Watch until the video is converted', async () => {
    const el = (await setup({ ...CONVERTED, converted: false })).nativeElement as HTMLElement;
    expect(el.querySelector('a.button.is-primary')).toBeNull();
    expect(el.textContent).toContain('Processing');
  });

  it('hides the Share action by default', async () => {
    const el = (await setup(CONVERTED)).nativeElement as HTMLElement;
    expect(el.querySelector('button')).toBeNull();
  });

  it('emits the video when the Share action is used', async () => {
    const fixture = await setup(CONVERTED, { shareable: true });
    const emitted: VideoInformation[] = [];
    fixture.componentInstance.share.subscribe((v) => emitted.push(v));

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(button.textContent).toContain('Share');
    button.click();

    expect(emitted).toEqual([CONVERTED]);
  });

  it('highlights the card when selected', async () => {
    const el = (await setup(CONVERTED, { selected: true })).nativeElement as HTMLElement;
    expect(el.querySelector('.card')?.classList).toContain('has-background-link-light');
  });

  it('renders the badge when provided and omits it when empty', async () => {
    const withBadge = (await setup(CONVERTED, { badge: 'Salsa' })).nativeElement as HTMLElement;
    const tag = withBadge.querySelector('.video-card__badge');
    expect(tag?.textContent?.trim()).toBe('Salsa');

    const withoutBadge = (await setup(CONVERTED)).nativeElement as HTMLElement;
    expect(withoutBadge.querySelector('.video-card__badge')).toBeNull();
  });

  it('assigns badge colors deterministically from the badge text', async () => {
    const first = (await setup(CONVERTED, { badge: 'Salsa' })).nativeElement as HTMLElement;
    const second = (await setup(CONVERTED, { badge: 'Salsa' })).nativeElement as HTMLElement;
    const different = (await setup(CONVERTED, { badge: 'Tango' })).nativeElement as HTMLElement;

    const firstBadge = first.querySelector('.video-card__badge') as HTMLElement;
    const secondBadge = second.querySelector('.video-card__badge') as HTMLElement;
    const differentBadge = different.querySelector('.video-card__badge') as HTMLElement;

    expect(firstBadge.dataset['badgeTone']).toBe(secondBadge.dataset['badgeTone']);
    expect(firstBadge.style.getPropertyValue('--video-card-badge-bg')).toBe(
      secondBadge.style.getPropertyValue('--video-card-badge-bg'),
    );
    expect(firstBadge.dataset['badgeTone']).not.toBe(differentBadge.dataset['badgeTone']);
  });
});
