import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { GroupsService } from '../../core/api/groups.service';
import { VideoFromGroupInformation } from '../../core/api/api-models';
import { VideoList } from '../../shared/ui/video-list/video-list';

interface GroupSection {
  readonly groupId: string;
  readonly groupName: string;
  readonly videos: VideoFromGroupInformation[];
}

@Component({
  selector: 'app-group-videos',
  imports: [VideoList],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './group-videos.html',
})
export class GroupVideos {
  private readonly groups = inject(GroupsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  private readonly items = signal<readonly VideoFromGroupInformation[]>([]);

  /** Recordings bucketed by their group, preserving first-seen order. */
  readonly sections = computed<GroupSection[]>(() => {
    const byGroup = new Map<string, GroupSection>();
    for (const video of this.items()) {
      const groupId = video.groupId ?? 'unknown';
      let section = byGroup.get(groupId);
      if (!section) {
        section = { groupId, groupName: video.groupName ?? 'Group', videos: [] };
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
