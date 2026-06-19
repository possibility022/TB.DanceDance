import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { AccessService } from '../../core/api/access.service';
import { EventModel, GroupModel } from '../../core/api/api-models';
import { LongDatePipe } from '../../shared/format/long-date.pipe';
import { SeasonPipe } from '../../shared/format/season.pipe';

@Component({
  selector: 'app-request-access',
  imports: [LongDatePipe, SeasonPipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './request-access.html',
})
export class RequestAccess {
  private readonly access = inject(AccessService);
  private readonly destroyRef = inject(DestroyRef);

  readonly today = new Date().toISOString().slice(0, 10);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly submitting = signal(false);

  readonly assignedGroups = signal<readonly GroupModel[]>([]);
  readonly assignedEvents = signal<readonly EventModel[]>([]);
  readonly availableGroups = signal<readonly GroupModel[]>([]);
  readonly availableEvents = signal<readonly EventModel[]>([]);
  readonly pendingCount = signal(0);

  /** Ids of assigned groups the user administers (surfaces a "Manage" link). */
  readonly administeredGroupIds = signal<ReadonlySet<string>>(new Set());

  isAdministered(groupId: string | undefined): boolean {
    return !!groupId && this.administeredGroupIds().has(groupId);
  }

  readonly checkedEvents = signal<ReadonlySet<string>>(new Set());
  readonly checkedGroups = signal<ReadonlySet<string>>(new Set());
  readonly groupDates = signal<Readonly<Record<string, string>>>({});

  readonly canSubmit = computed(() => this.checkedEvents().size > 0 || this.checkedGroups().size > 0);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.access
      .getMyAccess()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.assignedGroups.set(response.assigned?.groups ?? []);
          this.assignedEvents.set(response.assigned?.events ?? []);
          this.availableGroups.set(response.available?.groups ?? []);
          this.availableEvents.set(response.available?.events ?? []);
          this.pendingCount.set(
            (response.pending?.groups?.length ?? 0) + (response.pending?.events?.length ?? 0),
          );
          this.administeredGroupIds.set(new Set(response.administeredGroupIds ?? []));
          this.resetSelection();
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }

  toggleEvent(id: string | undefined): void {
    if (!id) {
      return;
    }
    this.checkedEvents.update((set) => toggle(set, id));
  }

  toggleGroup(group: GroupModel): void {
    const id = group.id;
    if (!id) {
      return;
    }
    this.checkedGroups.update((set) => toggle(set, id));
    if (this.checkedGroups().has(id) && !this.groupDates()[id]) {
      this.setGroupDate(id, this.seasonStartDate(group));
    }
  }

  setGroupDate(id: string, date: string): void {
    this.groupDates.update((dates) => ({ ...dates, [id]: date }));
  }

  /**
   * A group's season start as a `yyyy-MM-dd` string, used as the default join date so a member
   * sees the whole season. Falls back to today when the group has no season start. The API
   * returns `seasonStart` as an ISO string, so we slice it directly to avoid timezone drift.
   */
  seasonStartDate(group: GroupModel): string {
    const raw = group.seasonStart as unknown as string | Date | undefined;
    if (!raw) {
      return this.today;
    }
    return typeof raw === 'string' ? raw.slice(0, 10) : new Date(raw).toISOString().slice(0, 10);
  }

  submit(): void {
    if (!this.canSubmit() || this.submitting()) {
      return;
    }
    this.submitting.set(true);

    const dates = this.groupDates();
    const groupsById = new Map(this.availableGroups().map((group) => [group.id ?? '', group]));
    this.access
      .requestAccess({
        events: [...this.checkedEvents()],
        groups: [...this.checkedGroups()].map((id) => {
          const fallback = groupsById.has(id) ? this.seasonStartDate(groupsById.get(id)!) : this.today;
          return { id, joinedDate: new Date(dates[id] ?? fallback) };
        }),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.load();
        },
        error: () => this.submitting.set(false),
      });
  }

  private resetSelection(): void {
    this.checkedEvents.set(new Set());
    this.checkedGroups.set(new Set());
    this.groupDates.set({});
  }
}

function toggle(set: ReadonlySet<string>, id: string): ReadonlySet<string> {
  const next = new Set(set);
  if (next.has(id)) {
    next.delete(id);
  } else {
    next.add(id);
  }
  return next;
}
