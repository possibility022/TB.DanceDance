import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import { RouterLink } from '@angular/router';

import { AccessService } from '../../core/api/access.service';
import { GroupsService } from '../../core/api/groups.service';
import { GroupModel, VideoFromGroupInformation } from '../../core/api/api-models';
import { SeasonPipe } from '../../shared/format/season.pipe';
import { VideoList } from '../../shared/ui/video-list/video-list';

interface GroupSection {
  readonly groupId: string;
  readonly groupName: string;
  readonly seasonStart?: Date;
  readonly seasonEnd?: Date;
  readonly videos: VideoFromGroupInformation[];
}

@Component({
  selector: 'app-group-videos',
  imports: [RouterLink, SeasonPipe, VideoList],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './group-videos.html',
})
export class GroupVideos {
  private readonly groups = inject(GroupsService);
  private readonly access = inject(AccessService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  private readonly items = signal<readonly VideoFromGroupInformation[]>([]);
  private readonly seasons = signal<ReadonlyMap<string, GroupModel>>(new Map());

  /** Recordings bucketed by their group (with season), preserving first-seen order. */
  readonly sections = computed<GroupSection[]>(() => {
    const seasons = this.seasons();
    const byGroup = new Map<string, GroupSection>();
    for (const video of this.items()) {
      const groupId = video.groupId ?? 'unknown';
      let section = byGroup.get(groupId);
      if (!section) {
        const season = seasons.get(groupId);
        section = {
          groupId,
          groupName: video.groupName ?? 'Group',
          seasonStart: season?.seasonStart,
          seasonEnd: season?.seasonEnd,
          videos: [],
        };
        byGroup.set(groupId, section);
      }
      section.videos.push(video);
    }
    return [...byGroup.values()];
  });

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    forkJoin({
      videos: this.groups.getGroupVideos(),
      access: this.access.getMyAccess(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ videos, access }) => {
          this.items.set(videos.videos ?? []);
          const all = [...(access.assigned?.groups ?? []), ...(access.available?.groups ?? [])];
          this.seasons.set(new Map(all.filter((g) => g.id).map((g) => [g.id as string, g])));
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }
}
