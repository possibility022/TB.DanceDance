import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TransfersService } from '../../core/api/transfers.service';
import { TransferSummaryResponse } from '../../core/api/api-models';
import { FileSizePipe } from '../../shared/format/file-size.pipe';
import { LongDatePipe } from '../../shared/format/long-date.pipe';

/** Sender-facing list of the user's outgoing transfers, with revoke on pending ones and
 * roll-back on accepted ones still inside the rollback window. */
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
  private readonly rollingBackId = signal<string | null>(null);
  readonly copiedId = signal<string | null>(null);

  constructor() {
    this.load();
  }

  isPending(transfer: TransferSummaryResponse): boolean {
    return transfer.status === 'Pending';
  }

  isAccepted(transfer: TransferSummaryResponse): boolean {
    return transfer.status === 'Accepted';
  }

  isRollingBack(transfer: TransferSummaryResponse): boolean {
    return this.rollingBackId() === transfer.linkId;
  }

  /** Whether the rollback window is still open for this Accepted transfer. */
  canRollback(transfer: TransferSummaryResponse): boolean {
    return this.isAccepted(transfer) && !!transfer.rollbackDeadline && new Date() < new Date(transfer.rollbackDeadline);
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

  rollback(transfer: TransferSummaryResponse): void {
    const linkId = transfer.linkId;
    if (!linkId || this.rollingBackId() === linkId) {
      return;
    }
    this.rollingBackId.set(linkId);
    this.transfers
      .rollbackTransfer(linkId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.items.update((items) =>
            items.map((t) => (t.linkId === linkId ? { ...t, status: 'RolledBack' } : t)),
          );
          this.rollingBackId.set(null);
        },
        error: () => this.rollingBackId.set(null),
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
