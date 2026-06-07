import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { VideosService } from '../../core/api/videos.service';
import { VideoInformation } from '../../core/api/api-models';
import { VideoList } from '../../shared/ui/video-list/video-list';
import { ShareDialog } from '../sharing/share-dialog';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-my-videos',
  imports: [VideoList, ShareDialog],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './my-videos.html',
})
export class MyVideos {
  private readonly videos = inject(VideosService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly items = signal<readonly VideoInformation[]>([]);

  readonly loadingMore = signal(false);
  readonly canLoadMore = signal(false);
  private currentPage = 0;

  readonly shareTarget = signal<VideoInformation | null>(null);
  readonly shareOpen = signal(false);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.videos
      .getMyVideos(1, PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const items = response.items ?? [];
          this.items.set(items);
          this.currentPage = 1;
          this.canLoadMore.set(items.length < (response.totalCount ?? 0));
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }

  loadMore(): void {
    if (this.loadingMore() || !this.canLoadMore()) {
      return;
    }

    this.loadingMore.set(true);
    const nextPage = this.currentPage + 1;

    this.videos
      .getMyVideos(nextPage, PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const items = [...this.items(), ...(response.items ?? [])];
          this.items.set(items);
          this.currentPage = nextPage;
          this.canLoadMore.set(items.length < (response.totalCount ?? 0));
          this.loadingMore.set(false);
        },
        error: () => {
          this.loadingMore.set(false);
        },
      });
  }

  openShare(video: VideoInformation): void {
    this.shareTarget.set(video);
    this.shareOpen.set(true);
  }

  closeShare(): void {
    this.shareOpen.set(false);
  }
}
