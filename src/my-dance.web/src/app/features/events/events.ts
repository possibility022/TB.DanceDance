import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { AccessService } from '../../core/api/access.service';
import { EventsService } from '../../core/api/events.service';
import { EventModel, VideoInformation } from '../../core/api/api-models';
import { LongDatePipe } from '../../shared/format/long-date.pipe';
import { VideoList } from '../../shared/ui/video-list/video-list';

@Component({
  selector: 'app-events',
  imports: [ReactiveFormsModule, LongDatePipe, VideoList],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './events.html',
})
export class Events {
  private readonly access = inject(AccessService);
  private readonly events = inject(EventsService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly items = signal<readonly EventModel[]>([]);

  readonly selected = signal<EventModel | null>(null);
  readonly videos = signal<readonly VideoInformation[]>([]);
  readonly videosLoading = signal(false);
  readonly videosFailed = signal(false);

  readonly creating = signal(false);
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

  createEvent(): void {
    if (this.form.invalid || this.creating()) {
      return;
    }
    const { name, date } = this.form.getRawValue();
    this.creating.set(true);

    this.events
      .createEvent({ event: { name: name.trim(), date: new Date(date) } })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.creating.set(false);
          this.form.reset({ name: '', date: '' });
          this.load();
        },
        error: () => this.creating.set(false),
      });
  }
}
