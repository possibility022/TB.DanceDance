import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  input,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { TransfersService } from '../../core/api/transfers.service';
import { AcceptTransferResponse, TransferInfoResponse } from '../../core/api/api-models';
import { FileSizePipe } from '../../shared/format/file-size.pipe';
import { LongDatePipe } from '../../shared/format/long-date.pipe';

type Outcome = 'none' | 'accepted' | 'declined';

/** Recipient-facing landing for an incoming transfer, reached at /transfer/:linkId (auth-guarded). */
@Component({
  selector: 'app-transfer-landing',
  imports: [RouterLink, FileSizePipe, LongDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './transfer-landing.html',
  styles: `
    .transfer-preview {
      width: 100%;
      margin-top: 0.5rem;
      border-radius: 6px;
      background: #000;
    }
  `,
})
export class TransferLanding implements OnInit {
  /** Bound from the route `:linkId` param via withComponentInputBinding(). */
  readonly linkId = input.required<string>();

  private readonly transfers = inject(TransfersService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly info = signal<TransferInfoResponse | null>(null);
  private readonly streamUrls = signal<Record<string, string>>({});

  readonly submitting = signal(false);
  readonly outcome = signal<Outcome>('none');
  readonly quotaError = signal<{ required: number; available: number } | null>(null);
  readonly actionFailed = signal(false);

  ngOnInit(): void {
    this.load();
  }

  streamUrl(videoId: string | undefined): string | null {
    return videoId ? (this.streamUrls()[videoId] ?? null) : null;
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    forkJoin({
      info: this.transfers.getTransfer(this.linkId()),
      token: this.auth.getAccessToken(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ info, token }) => {
          this.info.set(info);
          const urls: Record<string, string> = {};
          for (const item of info.items ?? []) {
            if (item.videoId) {
              urls[item.videoId] = this.transfers.transferStreamUrl(this.linkId(), item.videoId, token);
            }
          }
          this.streamUrls.set(urls);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }

  accept(): void {
    if (this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.quotaError.set(null);
    this.actionFailed.set(false);

    this.transfers
      .acceptTransfer(this.linkId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response: AcceptTransferResponse) => {
          this.submitting.set(false);
          if (response.accepted) {
            this.outcome.set('accepted');
            // Re-fetch so info().rollbackDeadline reflects the freshly-accepted transfer.
            this.transfers
              .getTransfer(this.linkId())
              .pipe(takeUntilDestroyed(this.destroyRef))
              .subscribe((info) => this.info.set(info));
          } else {
            this.actionFailed.set(true);
          }
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          if (err.status === 409) {
            const body = err.error as AcceptTransferResponse | undefined;
            this.quotaError.set({
              required: body?.requiredBytes ?? 0,
              available: body?.availableBytes ?? 0,
            });
          } else {
            this.actionFailed.set(true);
          }
        },
      });
  }

  decline(): void {
    if (this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.actionFailed.set(false);

    this.transfers
      .declineTransfer(this.linkId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.outcome.set('declined');
        },
        error: () => {
          this.submitting.set(false);
          this.actionFailed.set(true);
        },
      });
  }
}
