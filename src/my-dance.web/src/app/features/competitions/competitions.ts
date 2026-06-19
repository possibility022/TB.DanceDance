import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CompetitionsService } from '../../core/api/competitions.service';
import { CompetitionSummaryResponse } from '../../core/api/api-models';
import { CommentVisibility, COMMENT_VISIBILITY_LABELS } from '../../shared/format/enums';
import { LongDatePipe } from '../../shared/format/long-date.pipe';

const VISIBILITY_OPTIONS = [
  CommentVisibility.LoggedInOnly,
  CommentVisibility.OwnerOnly,
  CommentVisibility.Everyone,
].map((value) => ({ value, label: COMMENT_VISIBILITY_LABELS[value] }));

/** List of the owner's competitions, with an inline create form. */
@Component({
  selector: 'app-competitions',
  imports: [RouterLink, ReactiveFormsModule, LongDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './competitions.html',
})
export class Competitions {
  private readonly competitions = inject(CompetitionsService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly items = signal<readonly CompetitionSummaryResponse[]>([]);
  readonly creating = signal(false);

  readonly visibilityOptions = VISIBILITY_OPTIONS;

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    location: [''],
    commentVisibility: [CommentVisibility.OwnerOnly as number],
  });

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

  create(): void {
    if (this.form.invalid || this.creating()) {
      return;
    }
    this.creating.set(true);
    const { name, location, commentVisibility } = this.form.getRawValue();
    this.competitions
      .createCompetition({ name, location: location || undefined, commentVisibility })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.creating.set(false);
          this.form.reset({ name: '', location: '', commentVisibility: CommentVisibility.OwnerOnly });
          this.load();
        },
        error: () => this.creating.set(false),
      });
  }
}
