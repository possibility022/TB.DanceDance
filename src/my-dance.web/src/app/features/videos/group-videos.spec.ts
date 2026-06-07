import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { GroupVideos } from './group-videos';
import { GroupsService } from '../../core/api/groups.service';
import { AccessService } from '../../core/api/access.service';
import { UploadService } from '../../core/api/upload.service';
import { BlobUploadService } from '../../core/api/blob-upload.service';
import { PagedResponseOfVideoFromGroupInformation } from '../../core/api/api-models';

async function setup(opts: {
  videos?: Observable<PagedResponseOfVideoFromGroupInformation>;
  getGroupVideos?: (
    page: number,
    pageSize: number,
  ) => Observable<PagedResponseOfVideoFromGroupInformation>;
}): Promise<ComponentFixture<GroupVideos>> {
  await TestBed.configureTestingModule({
    imports: [GroupVideos],
    providers: [
      provideRouter([]),
      {
        provide: GroupsService,
        useValue: {
          getGroupVideos: opts.getGroupVideos ?? (() => opts.videos ?? of({ items: [], totalCount: 0 })),
        },
      },
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
          items: [
            { videoId: 'v1', groupId: 'g1', groupName: 'Salsa', recordedDateTime: d(2026, 0, 10) },
            { videoId: 'v2', groupId: 'g2', groupName: 'Bachata', recordedDateTime: d(2026, 2, 1) },
            { videoId: 'v3', groupId: 'g1', groupName: 'Salsa', recordedDateTime: d(2026, 1, 15) },
          ],
          totalCount: 3,
        }),
      })
    ).componentInstance;

    expect(c.sortedVideos().map((v) => v.videoId)).toEqual(['v2', 'v3', 'v1']);
  });

  it('sinks videos without a recordedDateTime to the end of the list', async () => {
    const c = (
      await setup({
        videos: of({
          items: [
            { videoId: 'no-date' },
            { videoId: 'old', recordedDateTime: d(2024, 5, 1) },
            { videoId: 'new', recordedDateTime: d(2026, 5, 1) },
          ],
          totalCount: 3,
        }),
      })
    ).componentInstance;

    expect(c.sortedVideos().map((v) => v.videoId)).toEqual(['new', 'old', 'no-date']);
  });

  it('keeps group identity on each video so the card can render a badge and group-scoped link', async () => {
    const c = (
      await setup({
        videos: of({
          items: [{ videoId: 'v1', groupId: 'g1', groupName: 'Salsa', recordedDateTime: d(2026, 5, 1) }],
          totalCount: 1,
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

  it('exposes canLoadMore when there are more items than the first page returned', async () => {
    const c = (
      await setup({ videos: of({ items: [{ videoId: 'v1' }], totalCount: 3 }) })
    ).componentInstance;
    expect(c.canLoadMore()).toBe(true);
  });

  it('hides load more once every item has been loaded', async () => {
    const c = (
      await setup({ videos: of({ items: [{ videoId: 'v1' }, { videoId: 'v2' }], totalCount: 2 }) })
    ).componentInstance;
    expect(c.canLoadMore()).toBe(false);
  });

  it('loadMore appends the next page and tracks whether more remain', async () => {
    const calls: Array<[number, number]> = [];
    const getGroupVideos = (page: number, pageSize: number) => {
      calls.push([page, pageSize]);
      return page === 1
        ? of({ items: [{ videoId: 'v1' }], totalCount: 3, pageNumber: 1, pageSize: 1 })
        : of({ items: [{ videoId: 'v2' }], totalCount: 3, pageNumber: 2, pageSize: 1 });
    };

    const c = (await setup({ getGroupVideos })).componentInstance;
    expect(c.canLoadMore()).toBe(true);

    c.loadMore();

    expect(c.sortedVideos().map((v) => v.videoId)).toEqual(['v1', 'v2']);
    expect(c.canLoadMore()).toBe(true);
    expect(c.loadingMore()).toBe(false);
    expect(calls).toEqual([[1, 20], [2, 20]]);
  });

  it('loadMore is a no-op while already loading or when there is nothing more to load', async () => {
    let callCount = 0;
    const getGroupVideos = () => {
      callCount++;
      return of({ items: [{ videoId: 'v1' }, { videoId: 'v2' }], totalCount: 2 });
    };

    const c = (await setup({ getGroupVideos })).componentInstance;
    expect(callCount).toBe(1);
    expect(c.canLoadMore()).toBe(false);

    c.loadMore();

    expect(callCount).toBe(1);
  });
});
