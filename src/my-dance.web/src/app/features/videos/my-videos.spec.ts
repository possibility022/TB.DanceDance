import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { MyVideos } from './my-videos';
import { VideosService } from '../../core/api/videos.service';
import { SharingService } from '../../core/api/sharing.service';
import { TransfersService } from '../../core/api/transfers.service';
import { PagedResponseOfVideoInformation, VideoInformation } from '../../core/api/api-models';

async function setup(
  my?: Observable<PagedResponseOfVideoInformation>,
  getMyVideos?: (page: number, pageSize: number) => Observable<PagedResponseOfVideoInformation>,
): Promise<ComponentFixture<MyVideos>> {
  await TestBed.configureTestingModule({
    imports: [MyVideos],
    providers: [
      provideRouter([]),
      {
        provide: VideosService,
        useValue: {
          getMyVideos: getMyVideos ?? (() => my ?? of({ items: [], totalCount: 0 })),
          updateCommentSettings: () => of(void 0),
        },
      },
      {
        // Injected by the embedded ShareDialog; not exercised while it is closed.
        provide: SharingService,
        useValue: { getMySharedLinks: () => of({ links: [] }) },
      },
      {
        // Injected by the embedded CreateTransferDialog; not exercised while it is closed.
        provide: TransfersService,
        useValue: { createTransfer: () => of({ linkId: 't1' }) },
      },
    ],
  }).compileComponents();

  const fixture = TestBed.createComponent(MyVideos);
  fixture.detectChanges();
  return fixture;
}

describe('MyVideos', () => {
  it('loads the personal library', async () => {
    const c = (await setup(of({ items: [{ name: 'a' }, { name: 'b' }], totalCount: 2 }))).componentInstance;
    expect(c.loading()).toBe(false);
    expect(c.failed()).toBe(false);
    expect(c.items()).toHaveLength(2);
  });

  it('enters the failed state on error', async () => {
    const c = (await setup(throwError(() => new Error('boom')))).componentInstance;
    expect(c.failed()).toBe(true);
    expect(c.loading()).toBe(false);
  });

  it('exposes canLoadMore when there are more items than the first page returned', async () => {
    const c = (await setup(of({ items: [{ name: 'a' }], totalCount: 3 }))).componentInstance;
    expect(c.canLoadMore()).toBe(true);
  });

  it('hides load more once every item has been loaded', async () => {
    const c = (await setup(of({ items: [{ name: 'a' }, { name: 'b' }], totalCount: 2 }))).componentInstance;
    expect(c.canLoadMore()).toBe(false);
  });

  it('loadMore appends the next page and tracks whether more remain', async () => {
    const calls: Array<[number, number]> = [];
    const getMyVideos = (page: number, pageSize: number) => {
      calls.push([page, pageSize]);
      return page === 1
        ? of({ items: [{ name: 'a' }], totalCount: 3, pageNumber: 1, pageSize: 1 })
        : of({ items: [{ name: 'b' }], totalCount: 3, pageNumber: 2, pageSize: 1 });
    };

    const c = (await setup(undefined, getMyVideos)).componentInstance;
    expect(c.canLoadMore()).toBe(true);

    c.loadMore();

    expect(c.items().map((v) => v.name)).toEqual(['a', 'b']);
    expect(c.canLoadMore()).toBe(true);
    expect(c.loadingMore()).toBe(false);
    expect(calls).toEqual([[1, 20], [2, 20]]);
  });

  it('loadMore is a no-op while already loading or when there is nothing more to load', async () => {
    let callCount = 0;
    const getMyVideos = () => {
      callCount++;
      return of({ items: [{ name: 'a' }, { name: 'b' }], totalCount: 2 });
    };

    const c = (await setup(undefined, getMyVideos)).componentInstance;
    expect(callCount).toBe(1);
    expect(c.canLoadMore()).toBe(false);

    c.loadMore();

    expect(callCount).toBe(1);
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

  describe('multi-select transfer', () => {
    async function setupForSelect(): Promise<MyVideos> {
      const fixture = await setup(
        of({
          items: [
            { videoId: 'v1', name: 'A', sizeBytes: 1000 },
            { videoId: 'v2', name: 'B', sizeBytes: 2000 },
          ],
          totalCount: 2,
        }),
      );
      return fixture.componentInstance;
    }

    it('toggleSelectMode enables and clears selection', async () => {
      const c = await setupForSelect();
      c.toggleSelection({ videoId: 'v1' });
      expect(c.selectedIds()).toEqual(['v1']);

      c.toggleSelectMode(); // turn on
      expect(c.selectMode()).toBe(true);
      c.toggleSelectMode(); // turn off clears selection
      expect(c.selectMode()).toBe(false);
      expect(c.selectedIds()).toEqual([]);
    });

    it('toggleSelection adds and removes ids', async () => {
      const c = await setupForSelect();
      c.toggleSelection({ videoId: 'v1' });
      c.toggleSelection({ videoId: 'v2' });
      expect(c.selectedIds()).toEqual(['v1', 'v2']);
      c.toggleSelection({ videoId: 'v1' });
      expect(c.selectedIds()).toEqual(['v2']);
    });

    it('reports selected count and total size of selected videos', async () => {
      const c = await setupForSelect();
      c.toggleSelection({ videoId: 'v1' });
      c.toggleSelection({ videoId: 'v2' });
      expect(c.selectedCount()).toBe(2);
      expect(c.totalSelectedSize()).toBe(3000);
      expect(c.selectedVideos().map((v) => v.videoId)).toEqual(['v1', 'v2']);
    });

    it('openTransfer is a no-op when nothing is selected', async () => {
      const c = await setupForSelect();
      c.openTransfer();
      expect(c.transferOpen()).toBe(false);
    });

    it('openTransfer opens the dialog when something is selected', async () => {
      const c = await setupForSelect();
      c.toggleSelection({ videoId: 'v1' });
      c.openTransfer();
      expect(c.transferOpen()).toBe(true);
    });

    it('onTransferCreated clears selection and exits select mode', async () => {
      const c = await setupForSelect();
      c.toggleSelectMode();
      c.toggleSelection({ videoId: 'v1' });
      c.transferOpen.set(true);

      c.onTransferCreated();

      expect(c.transferOpen()).toBe(false);
      expect(c.selectMode()).toBe(false);
      expect(c.selectedIds()).toEqual([]);
    });
  });

  describe('delete', () => {
    async function setupForDelete(
      deleteVideo: (videoId: string) => Observable<void>,
    ): Promise<{ component: MyVideos }> {
      await TestBed.configureTestingModule({
        imports: [MyVideos],
        providers: [
          provideRouter([]),
          {
            provide: VideosService,
            useValue: {
              getMyVideos: () =>
                of({
                  items: [
                    { videoId: 'v1', name: 'A' },
                    { videoId: 'v2', name: 'B' },
                  ],
                  totalCount: 2,
                }),
              deleteVideo,
            },
          },
          { provide: SharingService, useValue: { getMySharedLinks: () => of({ links: [] }) } },
          { provide: TransfersService, useValue: { createTransfer: () => of({ linkId: 't1' }) } },
        ],
      }).compileComponents();

      const fixture = TestBed.createComponent(MyVideos);
      fixture.detectChanges();
      return { component: fixture.componentInstance };
    }

    afterEach(() => vi.restoreAllMocks());

    it('confirms, deletes, and drops the item from the list', async () => {
      vi.spyOn(window, 'confirm').mockReturnValue(true);
      const deleteVideo = vi.fn(() => of(void 0));
      const { component } = await setupForDelete(deleteVideo);

      component.onDelete({ videoId: 'v1', name: 'A' });

      expect(deleteVideo).toHaveBeenCalledWith('v1');
      expect(component.items().map((v) => v.videoId)).toEqual(['v2']);
    });

    it('is a no-op when the confirmation is dismissed', async () => {
      vi.spyOn(window, 'confirm').mockReturnValue(false);
      const deleteVideo = vi.fn(() => of(void 0));
      const { component } = await setupForDelete(deleteVideo);

      component.onDelete({ videoId: 'v1', name: 'A' });

      expect(deleteVideo).not.toHaveBeenCalled();
      expect(component.items().map((v) => v.videoId)).toEqual(['v1', 'v2']);
    });

    it('keeps the item when the delete request fails', async () => {
      vi.spyOn(window, 'confirm').mockReturnValue(true);
      const deleteVideo = vi.fn(() => throwError(() => new Error('boom')));
      const { component } = await setupForDelete(deleteVideo);

      component.onDelete({ videoId: 'v1', name: 'A' });

      expect(deleteVideo).toHaveBeenCalledWith('v1');
      expect(component.items().map((v) => v.videoId)).toEqual(['v1', 'v2']);
    });
  });
});
