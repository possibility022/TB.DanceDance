import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { VideosService } from '../../core/api/videos.service';
import { VideoInformation } from '../../core/api/api-models';
import { VideoList } from '../../shared/ui/video-list/video-list';
import { ShareDialog } from '../sharing/share-dialog';
import { TransferDialog } from '../transfers/transfer-dialog';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-my-videos',
  imports: [VideoList, ShareDialog, TransferDialog],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './my-videos.html',
})
export class MyVideos {
  private readonly videos = inject(VideosService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly doc = inject(DOCUMENT);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly items = signal<readonly VideoInformation[]>([]);
  private readonly deletingId = signal<string | null>(null);

  readonly loadingMore = signal(false);
  readonly canLoadMore = signal(false);
  private currentPage = 0;

  readonly shareTarget = signal<VideoInformation | null>(null);
  readonly shareOpen = signal(false);

  readonly transferTarget = signal<VideoInformation | null>(null);
  readonly transferOpen = signal(false);

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

  openTransfer(video: VideoInformation): void {
    this.transferTarget.set(video);
    this.transferOpen.set(true);
  }

  closeTransfer(): void {
    this.transferOpen.set(false);
  }

  onDelete(video: VideoInformation): void {
    const videoId = video.videoId;
    if (!videoId || this.deletingId() === videoId) {
      return;
    }

    const name = video.name || 'this recording';
    const confirmed = this.doc.defaultView?.confirm(
      `Delete “${name}”? This permanently removes the recording, its comments and any shared links.`,
    );
    if (!confirmed) {
      return;
    }

    this.deletingId.set(videoId);
    this.videos
      .deleteVideo(videoId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.items.update((items) => items.filter((v) => v.videoId !== videoId));
          this.deletingId.set(null);
        },
        error: () => this.deletingId.set(null),
      });
  }
}
