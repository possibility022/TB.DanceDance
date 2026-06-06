import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { RouterLink } from '@angular/router';

import { GroupsService } from '../../core/api/groups.service';
import { VideoFromGroupInformation } from '../../core/api/api-models';
import { VideoCard } from '../../shared/ui/video-card/video-card';
import { UploadDialog } from '../upload/upload-dialog';

function recordedTime(video: VideoFromGroupInformation): number {
  if (!video.recordedDateTime) {
    return -Infinity;
  }
  const time = new Date(video.recordedDateTime).getTime();
  return Number.isNaN(time) ? -Infinity : time;
}

@Component({
  selector: 'app-group-videos',
  imports: [RouterLink, VideoCard, UploadDialog],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './group-videos.html',
  styles: `
    .group-videos__header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    @media (max-width: 768px) {
      .group-videos__header {
        display: block;
      }
      .group-videos__header .button {
        margin-top: 0.75rem;
      }
    }
  `,
})
export class GroupVideos {
  private readonly groups = inject(GroupsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly uploadModalOpen = signal(false);
  private readonly items = signal<readonly VideoFromGroupInformation[]>([]);

  /** All lesson recordings, newest first; undated recordings sink to the end. */
  readonly sortedVideos = computed<readonly VideoFromGroupInformation[]>(() =>
    [...this.items()].sort((a, b) => recordedTime(b) - recordedTime(a)),
  );

  constructor() {
    this.load();
  }

  openUploadDialog(): void {
    this.uploadModalOpen.set(true);
  }

  closeUploadDialog(): void {
    this.uploadModalOpen.set(false);
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.groups
      .getGroupVideos()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.items.set(response.videos ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }
}
