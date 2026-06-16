import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { TransfersService } from '../../core/api/transfers.service';
import { TransferSummaryResponse, VideoInformation } from '../../core/api/api-models';
import { FileSizePipe } from '../../shared/format/file-size.pipe';

const DEFAULT_EXPIRATION_DAYS = 7;

/** Modal: create an ownership transfer for the selected recordings. */
@Component({
  selector: 'app-create-transfer-dialog',
  imports: [ReactiveFormsModule, FileSizePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './create-transfer-dialog.html',
})
export class CreateTransferDialog {
  readonly videos = input<readonly VideoInformation[]>([]);
  readonly open = input(false);
  readonly closed = output<void>();
  /** Emitted after a transfer is successfully created (so the parent can reset selection). */
  readonly created = output<TransferSummaryResponse>();

  private readonly transfers = inject(TransfersService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly form = this.fb.nonNullable.group({
    expirationDays: [
      DEFAULT_EXPIRATION_DAYS,
      [Validators.required, Validators.min(1), Validators.max(365)],
    ],
  });

  readonly creating = signal(false);
  readonly result = signal<TransferSummaryResponse | null>(null);
  readonly failed = signal(false);
  readonly copied = signal(false);

  readonly totalSizeBytes = computed(() =>
    this.videos().reduce((sum, v) => sum + (v.sizeBytes ?? 0), 0),
  );

  create(): void {
    if (this.form.invalid || this.creating() || this.videos().length === 0) {
      return;
    }
    const videoIds = this.videos()
      .map((v) => v.videoId)
      .filter((id): id is string => !!id);

    this.creating.set(true);
    this.failed.set(false);
    this.transfers
      .createTransfer({ videoIds, expirationDays: this.form.getRawValue().expirationDays })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.creating.set(false);
          this.result.set(response);
          this.created.emit(response);
        },
        error: () => {
          this.creating.set(false);
          this.failed.set(true);
        },
      });
  }

  copy(): void {
    const url = this.result()?.shareUrl;
    if (!url) {
      return;
    }
    void navigator.clipboard?.writeText(url).then(() => this.copied.set(true));
  }

  close(): void {
    this.result.set(null);
    this.failed.set(false);
    this.copied.set(false);
    this.form.reset({ expirationDays: DEFAULT_EXPIRATION_DAYS });
    this.closed.emit();
  }
}
