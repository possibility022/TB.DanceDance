import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { CompetitionsService } from '../../core/api/competitions.service';
import { CompetitionSummaryResponse } from '../../core/api/api-models';
import { LongDatePipe } from '../../shared/format/long-date.pipe';
import { CompetitionCreateDialog } from './competition-create-dialog';

/** List of the owner's competitions, with a create dialog. */
@Component({
  selector: 'app-competitions',
  imports: [RouterLink, LongDatePipe, CompetitionCreateDialog],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './competitions.html',
})
export class Competitions {
  private readonly competitions = inject(CompetitionsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly items = signal<readonly CompetitionSummaryResponse[]>([]);
  readonly createModalOpen = signal(false);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);
    this.competitions
      .getMyCompetitions()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.items.set(response.competitions ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }

  openCreateModal(): void {
    this.createModalOpen.set(true);
  }

  closeCreateModal(): void {
    this.createModalOpen.set(false);
  }

  onCompetitionCreated(): void {
    this.createModalOpen.set(false);
    this.load();
  }
}
