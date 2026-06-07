import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { GroupVideos } from './group-videos';
import { AuthService } from '../../core/auth/auth.service';
import { VideosService } from '../../core/api/videos.service';
import { GroupsService } from '../../core/api/groups.service';
import { AccessService } from '../../core/api/access.service';
import { UploadService } from '../../core/api/upload.service';
import { BlobUploadService } from '../../core/api/blob-upload.service';
import { ListGroupVideosResponse } from '../../core/api/api-models';

async function setup(opts: {
  videos?: Observable<ListGroupVideosResponse>;
}): Promise<ComponentFixture<GroupVideos>> {
  await TestBed.configureTestingModule({
    imports: [GroupVideos],
    providers: [
      provideRouter([]),
      { provide: AuthService, useValue: { getAccessToken: () => of('test-token') } },
      { provide: VideosService, useValue: { thumbnailUrl: () => null } },
      { provide: GroupsService, useValue: { getGroupVideos: () => opts.videos ?? of({ videos: [] }) } },
      { provide: AccessService, useValue: { getMyAccess: vi.fn(() => of({ assigned: { groups: [], events: [] } })) } },
      { provide: UploadService, useValue: { produceUploadUrl: vi.fn(() => of({ sas: '', videoId: 'v' })) } },
      { provide: BlobUploadService, useValue: { upload: vi.fn(() => of(100)) } },
    ],
  }).compileComponents();

  const fixture = TestBed.createComponent(GroupVideos);
  fixture.detectChanges();
  return fixture;
}

function d(y: number, m: number, day: number): Date {
  return new Date(y, m, day);
}

describe('GroupVideos', () => {
  it('flattens videos and sorts by recordedDateTime descending across groups', async () => {
    const c = (
      await setup({
        videos: of({
          videos: [
            { videoId: 'v1', groupId: 'g1', groupName: 'Salsa', recordedDateTime: d(2026, 0, 10) },
            { videoId: 'v2', groupId: 'g2', groupName: 'Bachata', recordedDateTime: d(2026, 2, 1) },
            { videoId: 'v3', groupId: 'g1', groupName: 'Salsa', recordedDateTime: d(2026, 1, 15) },
          ],
        }),
      })
    ).componentInstance;

    expect(c.sortedVideos().map((v) => v.videoId)).toEqual(['v2', 'v3', 'v1']);
  });

  it('sinks videos without a recordedDateTime to the end of the list', async () => {
    const c = (
      await setup({
        videos: of({
          videos: [
            { videoId: 'no-date' },
            { videoId: 'old', recordedDateTime: d(2024, 5, 1) },
            { videoId: 'new', recordedDateTime: d(2026, 5, 1) },
          ],
        }),
      })
    ).componentInstance;

    expect(c.sortedVideos().map((v) => v.videoId)).toEqual(['new', 'old', 'no-date']);
  });

  it('keeps group identity on each video so the card can render a badge and group-scoped link', async () => {
    const c = (
      await setup({
        videos: of({
          videos: [{ videoId: 'v1', groupId: 'g1', groupName: 'Salsa', recordedDateTime: d(2026, 5, 1) }],
        }),
      })
    ).componentInstance;

    const [video] = c.sortedVideos();
    expect(video.groupId).toBe('g1');
    expect(video.groupName).toBe('Salsa');
  });

  it('enters the failed state on error', async () => {
    const c = (await setup({ videos: throwError(() => new Error('boom')) })).componentInstance;
    expect(c.failed()).toBe(true);
    expect(c.loading()).toBe(false);
  });

  it('openUploadDialog() and closeUploadDialog() toggle the upload modal', async () => {
    const component = (await setup({})).componentInstance;

    expect(component.uploadModalOpen()).toBe(false);
    component.openUploadDialog();
    expect(component.uploadModalOpen()).toBe(true);
    component.closeUploadDialog();
    expect(component.uploadModalOpen()).toBe(false);
  });
});
