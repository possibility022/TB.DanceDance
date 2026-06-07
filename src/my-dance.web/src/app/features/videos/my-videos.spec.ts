import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { MyVideos } from './my-videos';
import { VideosService } from '../../core/api/videos.service';
import { SharingService } from '../../core/api/sharing.service';
import { MyVideosResponse, VideoInformation } from '../../core/api/api-models';

async function setup(my?: Observable<MyVideosResponse>): Promise<ComponentFixture<MyVideos>> {
  await TestBed.configureTestingModule({
    imports: [MyVideos],
    providers: [
      provideRouter([]),
      {
        provide: VideosService,
        useValue: {
          getMyVideos: () => my ?? of({ videoInformation: [] }),
          updateCommentSettings: () => of(void 0),
        },
      },
      {
        // Injected by the embedded ShareDialog; not exercised while it is closed.
        provide: SharingService,
        useValue: { getMySharedLinks: () => of({ links: [] }) },
      },
    ],
  }).compileComponents();

  const fixture = TestBed.createComponent(MyVideos);
  fixture.detectChanges();
  return fixture;
}

describe('MyVideos', () => {
  it('loads the personal library', async () => {
    const c = (await setup(of({ videoInformation: [{ name: 'a' }, { name: 'b' }] }))).componentInstance;
    expect(c.loading()).toBe(false);
    expect(c.failed()).toBe(false);
    expect(c.items()).toHaveLength(2);
  });

  it('enters the failed state on error', async () => {
    const c = (await setup(throwError(() => new Error('boom')))).componentInstance;
    expect(c.failed()).toBe(true);
    expect(c.loading()).toBe(false);
  });

  it('openShare targets a video and opens the dialog', async () => {
    const c = (await setup()).componentInstance;
    const video: VideoInformation = { videoId: 'v1', name: 'Tango' };

    c.openShare(video);

    expect(c.shareTarget()).toBe(video);
    expect(c.shareOpen()).toBe(true);
  });

  it('closeShare hides the dialog', async () => {
    const c = (await setup()).componentInstance;
    c.openShare({ videoId: 'v1' });
    c.closeShare();
    expect(c.shareOpen()).toBe(false);
  });
});
