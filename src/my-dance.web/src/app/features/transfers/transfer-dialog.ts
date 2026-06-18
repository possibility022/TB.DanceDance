import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { TransfersService } from '../../core/api/transfers.service';
import { TransferSummaryResponse } from '../../core/api/api-models';

const DEFAULT_EXPIRATION_DAYS = 7;

/** Modal: create a transfer link that gives ownership of a single recording to the recipient. */
@Component({
  selector: 'app-transfer-dialog',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transfer-dialog.html',
})
export class TransferDialog {
  readonly videoId = input<string>('');
  readonly videoName = input<string>('');
  readonly open = input(false);
  readonly closed = output<void>();

  private readonly transfers = inject(TransfersService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly transferForm = this.fb.nonNullable.group({
    expirationDays: [
      DEFAULT_EXPIRATION_DAYS,
      [Validators.required, Validators.min(1), Validators.max(365)],
    ],
  });

  readonly transferring = signal(false);
  readonly transferFailed = signal(false);
  readonly transferResult = signal<TransferSummaryResponse | null>(null);
  readonly copied = signal(false);

  readonly existingTransfer = signal<TransferSummaryResponse | null>(null);
  readonly revoking = signal(false);

  readonly shareUrl = computed(() => {
    const result = this.transferResult();
    if (!result) return '';
    const url = result.shareUrl ?? '';
    if (/^https?:\/\//i.test(url)) return url;
    return `${window.location.origin}/transfer/${result.linkId ?? ''}`;
  });

  constructor() {
    effect(() => {
      if (this.open() && this.videoId()) {
        this.loadExisting();
      }
    });
  }

  transfer(): void {
    if (this.transferForm.invalid || this.transferring() || !this.videoId()) return;
    this.transferring.set(true);
    this.transferFailed.set(false);
    this.transfers
      .createTransfer(this.videoId(), this.transferForm.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.transferring.set(false);
          this.transferResult.set(result);
        },
        error: () => {
          this.transferring.set(false);
          this.transferFailed.set(true);
        },
      });
  }

  revoke(): void {
    const linkId = this.existingTransfer()?.linkId;
    if (!linkId || this.revoking()) return;
    this.revoking.set(true);
    this.transfers
      .revokeTransfer(linkId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.existingTransfer.set(null);
          this.revoking.set(false);
        },
        error: () => this.revoking.set(false),
      });
  }

  copy(): void {
    const url = this.shareUrl();
    if (!url) return;
    void navigator.clipboard?.writeText(url).then(() => this.copied.set(true));
  }

  close(): void {
    this.transferResult.set(null);
    this.transferFailed.set(false);
    this.copied.set(false);
    this.existingTransfer.set(null);
    this.revoking.set(false);
    this.transferForm.reset({ expirationDays: DEFAULT_EXPIRATION_DAYS });
    this.closed.emit();
  }

  private loadExisting(): void {
    this.transfers
      .getMyTransfers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const found = (response.transfers ?? []).find(
            (t) => t.status === 'Pending' && t.items?.some((i) => i.videoId === this.videoId()),
          );
          this.existingTransfer.set(found ?? null);
        },
        error: () => this.existingTransfer.set(null),
      });
  }
}
