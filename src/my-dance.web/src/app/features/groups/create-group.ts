import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { GroupsService } from '../../core/api/groups.service';

/** Create a new group. The creator becomes its first admin and lands on its management screen. */
@Component({
  selector: 'app-create-group',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './create-group.html',
})
export class CreateGroup {
  private readonly groups = inject(GroupsService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly submitting = signal(false);
  readonly failed = signal(false);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    seasonStart: ['', Validators.required],
    seasonEnd: ['', Validators.required],
  });

  /** True when both dates are set and the start is after the end. */
  readonly seasonOrderInvalid = computed(() => {
    const { seasonStart, seasonEnd } = this.form.getRawValue();
    return !!seasonStart && !!seasonEnd && seasonStart > seasonEnd;
  });

  submit(): void {
    if (this.form.invalid || this.seasonOrderInvalid() || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.failed.set(false);

    const { name, seasonStart, seasonEnd } = this.form.getRawValue();
    this.groups
      .createGroup({
        name,
        seasonStart: new Date(seasonStart),
        seasonEnd: new Date(seasonEnd),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.submitting.set(false);
          if (response.id) {
            void this.router.navigate(['/groups', response.id, 'manage']);
          }
        },
        error: () => {
          this.submitting.set(false);
          this.failed.set(true);
        },
      });
  }
}
