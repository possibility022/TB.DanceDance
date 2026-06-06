import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { RouterLink } from '@angular/router';

import { GroupsService } from '../../core/api/groups.service';
import { VideoFromGroupInformation } from '../../core/api/api-models';
import { VideoCard } from '../../shared/ui/video-card/video-card';

function recordedTime(video: VideoFromGroupInformation): number {
  if (!video.recordedDateTime) {
    return -Infinity;
  }
  const time = new Date(video.recordedDateTime).getTime();
  return Number.isNaN(time) ? -Infinity : time;
}

@Component({
  selector: 'app-group-videos',
  imports: [RouterLink, VideoCard],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './group-videos.html',
})
export class GroupVideos {
  private readonly groups = inject(GroupsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  private readonly items = signal<readonly VideoFromGroupInformation[]>([]);

  /** All lesson recordings, newest first; undated recordings sink to the end. */
  readonly sortedVideos = computed<readonly VideoFromGroupInformation[]>(() =>
    [...this.items()].sort((a, b) => recordedTime(b) - recordedTime(a)),
  );

  constructor() {
    this.load();
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
