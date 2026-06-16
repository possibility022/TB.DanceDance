import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { VideosService } from '../../core/api/videos.service';
import { VideoInformation } from '../../core/api/api-models';
import { VideoList } from '../../shared/ui/video-list/video-list';
import { ShareDialog } from '../sharing/share-dialog';
import { CreateTransferDialog } from '../transfers/create-transfer-dialog';
import { FileSizePipe } from '../../shared/format/file-size.pipe';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-my-videos',
  imports: [VideoList, ShareDialog, CreateTransferDialog, FileSizePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './my-videos.html',
  styles: `
    .transfer-action-bar {
      position: sticky;
      bottom: 0;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      margin-top: 1rem;
      padding: 0.75rem 1rem;
      border-radius: 8px;
      background: var(--bulma-scheme-main-bis, #fff);
      box-shadow: 0 -0.4rem 1rem rgba(20, 26, 44, 0.12);
    }
  `,
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

  // Multi-select / transfer state.
  readonly selectMode = signal(false);
  readonly selectedIds = signal<readonly string[]>([]);
  readonly transferOpen = signal(false);

  readonly selectedVideos = computed(() => {
    const ids = new Set(this.selectedIds());
    return this.items().filter((v) => !!v.videoId && ids.has(v.videoId));
  });
  readonly selectedCount = computed(() => this.selectedIds().length);
  readonly totalSelectedSize = computed(() =>
    this.selectedVideos().reduce((sum, v) => sum + (v.sizeBytes ?? 0), 0),
  );

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

  toggleSelectMode(): void {
    const next = !this.selectMode();
    this.selectMode.set(next);
    if (!next) {
      this.selectedIds.set([]);
    }
  }

  toggleSelection(video: VideoInformation): void {
    const id = video.videoId;
    if (!id) {
      return;
    }
    this.selectedIds.update((ids) =>
      ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id],
    );
  }

  openTransfer(): void {
    if (this.selectedCount() === 0) {
      return;
    }
    this.transferOpen.set(true);
  }

  closeTransfer(): void {
    this.transferOpen.set(false);
  }

  onTransferCreated(): void {
    // The transferred recordings will leave this library once accepted; for now just
    // clear the selection and close out of select mode.
    this.transferOpen.set(false);
    this.selectMode.set(false);
    this.selectedIds.set([]);
  }

  openShare(video: VideoInformation): void {
    this.shareTarget.set(video);
    this.shareOpen.set(true);
  }

  closeShare(): void {
    this.shareOpen.set(false);
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
