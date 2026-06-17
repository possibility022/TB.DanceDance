import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TransfersService } from '../../core/api/transfers.service';
import { AcceptTransferResponse, TransferSummaryResponse } from '../../core/api/api-models';
import { FileSizePipe } from '../../shared/format/file-size.pipe';
import { LongDatePipe } from '../../shared/format/long-date.pipe';

/** Sender-facing list of the user's outgoing transfers, with revoke on pending ones. */
@Component({
  selector: 'app-my-transfers',
  imports: [FileSizePipe, LongDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './my-transfers.html',
})
export class MyTransfers {
  private readonly transfers = inject(TransfersService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly items = signal<readonly TransferSummaryResponse[]>([]);
  private readonly revokingId = signal<string | null>(null);
  private readonly approvingId = signal<string | null>(null);
  private readonly cancellingId = signal<string | null>(null);
  readonly copiedId = signal<string | null>(null);

  /** Per-transfer inline quota error from a failed Approve (409). */
  readonly approveQuotaError = signal<{ linkId: string; required: number; available: number } | null>(null);

  constructor() {
    this.load();
  }

  isPending(transfer: TransferSummaryResponse): boolean {
    return transfer.status === 'Pending';
  }

  isAccepted(transfer: TransferSummaryResponse): boolean {
    return transfer.status === 'Accepted';
  }

  isApproving(transfer: TransferSummaryResponse): boolean {
    return this.approvingId() === transfer.linkId;
  }

  isCancelling(transfer: TransferSummaryResponse): boolean {
    return this.cancellingId() === transfer.linkId;
  }

  /** Absolute, copyable transfer URL — falls back to the current origin if the API returned a relative url. */
  shareUrl(transfer: TransferSummaryResponse): string {
    const url = transfer.shareUrl ?? '';
    if (/^https?:\/\//i.test(url)) {
      return url;
    }
    return `${window.location.origin}/transfer/${transfer.linkId ?? ''}`;
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.transfers
      .getMyTransfers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.items.set(response.transfers ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }

  revoke(transfer: TransferSummaryResponse): void {
    const linkId = transfer.linkId;
    if (!linkId || this.revokingId() === linkId) {
      return;
    }
    this.revokingId.set(linkId);
    this.transfers
      .revokeTransfer(linkId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.items.update((items) =>
            items.map((t) => (t.linkId === linkId ? { ...t, status: 'Revoked' } : t)),
          );
          this.revokingId.set(null);
        },
        error: () => this.revokingId.set(null),
      });
  }

  approve(transfer: TransferSummaryResponse): void {
    const linkId = transfer.linkId;
    if (!linkId || this.approvingId() === linkId) {
      return;
    }
    this.approveQuotaError.set(null);
    this.approvingId.set(linkId);
    this.transfers
      .approveTransfer(linkId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response: AcceptTransferResponse) => {
          this.approvingId.set(null);
          if (response.accepted) {
            this.items.update((items) =>
              items.map((t) => (t.linkId === linkId ? { ...t, status: 'Approved' } : t)),
            );
          }
        },
        error: (err: HttpErrorResponse) => {
          this.approvingId.set(null);
          if (err.status === 409) {
            const body = err.error as AcceptTransferResponse | undefined;
            this.approveQuotaError.set({
              linkId,
              required: body?.requiredBytes ?? 0,
              available: body?.availableBytes ?? 0,
            });
          }
        },
      });
  }

  cancel(transfer: TransferSummaryResponse): void {
    const linkId = transfer.linkId;
    if (!linkId || this.cancellingId() === linkId) {
      return;
    }
    this.cancellingId.set(linkId);
    this.transfers
      .cancelTransfer(linkId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.items.update((items) =>
            items.map((t) => (t.linkId === linkId ? { ...t, status: 'Cancelled' } : t)),
          );
          this.cancellingId.set(null);
          this.approveQuotaError.update((e) => (e?.linkId === linkId ? null : e));
        },
        error: () => this.cancellingId.set(null),
      });
  }

  copy(transfer: TransferSummaryResponse): void {
    const url = this.shareUrl(transfer);
    if (!url) {
      return;
    }
    void navigator.clipboard?.writeText(url).then(() => {
      this.copiedId.set(transfer.linkId ?? null);
    });
  }
}
