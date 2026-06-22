import { ChangeDetectionStrategy, Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { distinctUntilChanged, filter } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { CompetitionsService } from '../../core/api/competitions.service';
import { CommentVisibility, COMMENT_VISIBILITY_LABELS } from '../../shared/format/enums';

const VISIBILITY_OPTIONS = [
  CommentVisibility.LoggedInOnly,
  CommentVisibility.OwnerOnly,
  CommentVisibility.Everyone,
].map((value) => ({ value, label: COMMENT_VISIBILITY_LABELS[value] }));

/** Modal form for creating a new competition. */
@Component({
  selector: 'app-competition-create-dialog',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './competition-create-dialog.html',
})
export class CompetitionCreateDialog {
  private readonly competitions = inject(CompetitionsService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly open = input(false);
  readonly closed = output<void>();
  readonly created = output<void>();

  readonly creating = signal(false);
  readonly visibilityOptions = VISIBILITY_OPTIONS;

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    location: [''],
    commentVisibility: [CommentVisibility.OwnerOnly as number],
  });

  constructor() {
    toObservable(this.open)
      .pipe(
        distinctUntilChanged(),
        filter((isOpen) => isOpen),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.resetForm());
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
          this.resetForm();
          this.created.emit();
        },
        error: () => this.creating.set(false),
      });
  }

  close(): void {
    this.resetForm();
    this.closed.emit();
  }

  private resetForm(): void {
    this.form.reset({ name: '', location: '', commentVisibility: CommentVisibility.OwnerOnly });
  }
}
