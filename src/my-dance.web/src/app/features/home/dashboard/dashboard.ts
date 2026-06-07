import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { AccessService } from '../../../core/api/access.service';
import { GroupsService } from '../../../core/api/groups.service';
import { VideosService } from '../../../core/api/videos.service';
import { EventModel, VideoInformation } from '../../../core/api/api-models';
import { LongDatePipe } from '../../../shared/format/long-date.pipe';

const RECENT_LIMIT = 5;

function recordedTime(video: VideoInformation): number {
  return video.recordedDateTime ? new Date(video.recordedDateTime).getTime() : Number.NaN;
}

function latestDate(videos: readonly VideoInformation[]): Date | null {
  let max: number | null = null;
  for (const video of videos) {
    const time = recordedTime(video);
    if (!Number.isNaN(time) && (max === null || time > max)) {
      max = time;
    }
  }
  return max === null ? null : new Date(max);
}

/** Signed-in overview: library summaries, quick actions, and recent activity. */
@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, LongDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.html',
})
export class Dashboard {
  private readonly videos = inject(VideosService);
  private readonly groups = inject(GroupsService);
  private readonly access = inject(AccessService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);

  private readonly groupVideos = signal<readonly VideoInformation[]>([]);
  private readonly myVideos = signal<readonly VideoInformation[]>([]);
  private readonly events = signal<readonly EventModel[]>([]);

  readonly groupCount = computed(() => this.groupVideos().length);
  readonly myCount = computed(() => this.myVideos().length);
  readonly eventCount = computed(() => this.events().length);

  readonly groupLatest = computed(() => latestDate(this.groupVideos()));
  readonly myLatest = computed(() => latestDate(this.myVideos()));

  readonly recent = computed(() =>
    [...this.groupVideos(), ...this.myVideos()]
      .filter((video) => !Number.isNaN(recordedTime(video)))
      .sort((a, b) => recordedTime(b) - recordedTime(a))
      .slice(0, RECENT_LIMIT),
  );

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    forkJoin({
      my: this.videos.getMyVideos(1, RECENT_LIMIT),
      groups: this.groups.getGroupVideos(),
      access: this.access.getMyAccess(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ my, groups, access }) => {
          this.myVideos.set(my.items ?? []);
          this.groupVideos.set(groups.videos ?? []);
          this.events.set(access.assigned?.events ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }
}
