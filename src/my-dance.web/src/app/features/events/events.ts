import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { AccessService } from '../../core/api/access.service';
import { EventsService } from '../../core/api/events.service';
import { EventModel, VideoInformation } from '../../core/api/api-models';
import { LongDatePipe } from '../../shared/format/long-date.pipe';
import { VideoList } from '../../shared/ui/video-list/video-list';

interface EventSeasonGroup {
  readonly key: string;
  readonly label: string;
  readonly events: readonly EventModel[];
}

type Season = 'Winter' | 'Spring' | 'Summer' | 'Autumn';

@Component({
  selector: 'app-events',
  imports: [ReactiveFormsModule, LongDatePipe, VideoList],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './events.html',
  styles: `
    .events-page__header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1.5rem;
    }

    .events-page__header .subtitle {
      margin-bottom: 0;
    }

    .events-page__panel {
      border: 1px solid color-mix(in srgb, var(--bulma-border) 82%, transparent);
      border-radius: 8px;
      box-shadow: 0 0.35rem 1rem rgba(20, 26, 44, 0.06);
    }

    .events-page__event-list-panel {
      max-width: 54rem;
    }

    .events-page__panel-header,
    .events-page__detail-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 1rem;
    }

    .events-page__section-label {
      margin: 1rem 0 0.5rem;
      color: var(--bulma-text-weak);
      font-size: 0.72rem;
      font-weight: 800;
      letter-spacing: 0.08em;
      text-transform: uppercase;
    }

    .events-page__list {
      display: grid;
      gap: 0.5rem;
    }

    .events-page__recordings-panel {
      width: 100%;
    }

    .events-page__event-button {
      display: flex;
      width: 100%;
      min-height: 4.25rem;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
      padding: 0.75rem;
      border: 1px solid var(--bulma-border);
      border-radius: 8px;
      background: var(--bulma-scheme-main);
      color: inherit;
      text-align: left;
      cursor: pointer;
    }

    .events-page__event-button:hover,
    .events-page__event-button:focus-visible {
      border-color: color-mix(in srgb, var(--bulma-primary) 42%, var(--bulma-border));
      box-shadow: 0 0.45rem 1rem rgba(20, 26, 44, 0.08);
      outline: none;
    }

    .events-page__event-name {
      display: block;
      font-weight: 800;
      line-height: 1.25;
    }

    .events-page__event-date {
      display: block;
      margin-top: 0.2rem;
    }

    .events-page__event-arrow {
      flex: 0 0 auto;
      color: var(--bulma-text-weak);
      font-weight: 800;
    }

    .events-page__modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.5rem;
    }

    @media (max-width: 768px) {
      .events-page__header,
      .events-page__panel-header,
      .events-page__detail-header {
        display: block;
      }

      .events-page__header .button,
      .events-page__detail-header .button,
      .events-page__detail-header .tag {
        margin-top: 0.75rem;
      }
    }
  `,
})
export class Events {
  private readonly access = inject(AccessService);
  private readonly events = inject(EventsService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly items = signal<readonly EventModel[]>([]);
  readonly eventSeasonGroups = computed(() => groupEventsBySeason(this.items()));

  readonly selected = signal<EventModel | null>(null);
  readonly videos = signal<readonly VideoInformation[]>([]);
  readonly videosLoading = signal(false);
  readonly videosFailed = signal(false);

  readonly creating = signal(false);
  readonly createModalOpen = signal(false);
  readonly createFailed = signal(false);
  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    date: ['', [Validators.required]],
  });

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
          this.items.set(response.assigned?.events ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }

  select(event: EventModel): void {
    this.selected.set(event);
    if (!event.id) {
      return;
    }
    this.videosLoading.set(true);
    this.videosFailed.set(false);
    this.videos.set([]);

    this.events
      .getEventVideos(event.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.videos.set(response.videos ?? []);
          this.videosLoading.set(false);
        },
        error: () => {
          this.videosFailed.set(true);
          this.videosLoading.set(false);
        },
      });
  }

  clearSelection(): void {
    this.selected.set(null);
    this.videos.set([]);
    this.videosLoading.set(false);
    this.videosFailed.set(false);
  }

  openCreateModal(): void {
    this.createFailed.set(false);
    this.createModalOpen.set(true);
  }

  closeCreateModal(): void {
    if (this.creating()) {
      return;
    }
    this.createFailed.set(false);
    this.createModalOpen.set(false);
    this.form.reset({ name: '', date: '' });
  }

  createEvent(): void {
    if (this.form.invalid || this.creating()) {
      return;
    }
    const { name, date } = this.form.getRawValue();
    const trimmedName = name.trim();
    if (!trimmedName) {
      this.form.controls.name.setErrors({ required: true });
      return;
    }

    this.creating.set(true);
    this.createFailed.set(false);

    this.events
      .createEvent({ event: { name: trimmedName, date: new Date(date) } })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.creating.set(false);
          this.createModalOpen.set(false);
          this.form.reset({ name: '', date: '' });
          this.load();
        },
        error: () => {
          this.createFailed.set(true);
          this.creating.set(false);
        },
      });
  }
}

function dateValue(value: Date | string | number | undefined): number {
  if (!value) {
    return 0;
  }
  return new Date(value).getTime();
}

function groupEventsBySeason(events: readonly EventModel[]): readonly EventSeasonGroup[] {
  const groups = new Map<
    string,
    { label: string; newestEventDate: number; events: EventModel[] }
  >();

  for (const event of events) {
    const date = toDate(event.date);
    const year = date.getFullYear();
    const season = seasonForMonth(date.getMonth());
    const key = `${year}-${season}`;
    const eventDate = date.getTime();
    const group = groups.get(key);

    if (group) {
      group.events.push(event);
      group.newestEventDate = Math.max(group.newestEventDate, eventDate);
    } else {
      groups.set(key, {
        label: `${year} ${season}`,
        newestEventDate: eventDate,
        events: [event],
      });
    }
  }

  return [...groups.entries()]
    .map(([key, group]) => ({
      key,
      label: group.label,
      newestEventDate: group.newestEventDate,
      events: group.events.sort((a, b) => dateValue(b.date) - dateValue(a.date)),
    }))
    .sort((a, b) => b.newestEventDate - a.newestEventDate)
    .map(({ key, label, events }) => ({ key, label, events }));
}

function toDate(value: Date | string | number | undefined): Date {
  return value ? new Date(value) : new Date(0);
}

function seasonForMonth(month: number): Season {
  if (month >= 2 && month <= 4) {
    return 'Spring';
  }
  if (month >= 5 && month <= 7) {
    return 'Summer';
  }
  if (month >= 8 && month <= 10) {
    return 'Autumn';
  }
  return 'Winter';
}
