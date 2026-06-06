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
  inputs: Partial<{ shareable: boolean; selected: boolean; queryParams: Record<string, string> }> = {},
): Promise<ComponentFixture<VideoCard>> {
  await TestBed.configureTestingModule({
    imports: [VideoCard],
    providers: [provideRouter([])],
  }).compileComponents();

  const fixture = TestBed.createComponent(VideoCard);
  fixture.componentRef.setInput('video', video);
  for (const [key, value] of Object.entries(inputs)) {
    fixture.componentRef.setInput(key, value);
  }
  fixture.detectChanges();
  return fixture;
}

describe('VideoCard', () => {
  it('renders the name, formatted recorded date, and duration', async () => {
    const el = (await setup(CONVERTED)).nativeElement as HTMLElement;
    expect(el.querySelector('h3')?.textContent).toContain('Waltz basics');
    const meta = el.querySelector('p')?.textContent ?? '';
    expect(meta).toContain('04 June 2026');
    expect(meta).toContain('3:21');
  });

  it('links Watch to the player using the blob id and query params', async () => {
    const el = (await setup(CONVERTED, { queryParams: { groupId: 'g1' } })).nativeElement as HTMLElement;
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
});
