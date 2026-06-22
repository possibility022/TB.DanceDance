import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { GroupsService } from '../../core/api/groups.service';
import { GroupModel } from '../../core/api/api-models';
import { SeasonRangePipe } from '../../shared/format/season-range.pipe';

/** Lists the groups the current user administers; selecting one opens its management screen. */
@Component({
  selector: 'app-groups-list',
  imports: [RouterLink, SeasonRangePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './groups-list.html',
})
export class GroupsList implements OnInit {
  private readonly groups = inject(GroupsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly items = signal<readonly GroupModel[]>([]);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.groups
      .listMyGroups()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.items.set(response.groups ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }
}
