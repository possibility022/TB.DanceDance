import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { SharingService } from '../../core/api/sharing.service';
import { SharedLinkResponse } from '../../core/api/api-models';
import { LongDatePipe } from '../../shared/format/long-date.pipe';

const DEFAULT_EXPIRATION_DAYS = 7;

/** Modal: create and manage shared links for a single recording. */
@Component({
  selector: 'app-share-dialog',
  imports: [ReactiveFormsModule, LongDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './share-dialog.html',
})
export class ShareDialog {
  readonly videoId = input<string>('');
  readonly videoName = input<string>('');
  readonly open = input(false);
  readonly closed = output<void>();

  private readonly sharing = inject(SharingService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly form = this.fb.nonNullable.group({
    expirationDays: [DEFAULT_EXPIRATION_DAYS, [Validators.required, Validators.min(1), Validators.max(365)]],
    allowComments: [true],
    allowAnonymousComments: [false],
  });

  readonly links = signal<readonly SharedLinkResponse[]>([]);
  readonly creating = signal(false);
  readonly copiedLinkId = signal<string | null>(null);

  constructor() {
    effect(() => {
      if (this.open() && this.videoId()) {
        this.loadLinks();
      }
    });
  }

  shareUrl(link: SharedLinkResponse): string {
    return link.shareUrl || `${window.location.origin}/shared/${link.linkId}`;
  }

  create(): void {
    if (this.form.invalid || this.creating()) {
      return;
    }
    this.creating.set(true);
    this.sharing
      .createSharedLink(this.videoId(), this.form.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.creating.set(false);
          this.loadLinks();
        },
        error: () => this.creating.set(false),
      });
  }

  revoke(link: SharedLinkResponse): void {
    if (!link.linkId) {
      return;
    }
    this.sharing
      .revokeSharedLink(link.linkId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: () => this.loadLinks() });
  }

  copy(link: SharedLinkResponse): void {
    void navigator.clipboard?.writeText(this.shareUrl(link)).then(() => {
      this.copiedLinkId.set(link.linkId ?? null);
    });
  }

  close(): void {
    this.copiedLinkId.set(null);
    this.closed.emit();
  }

  private loadLinks(): void {
    this.sharing
      .getMySharedLinks()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) =>
          this.links.set((response.links ?? []).filter((link) => link.videoId === this.videoId())),
        error: () => this.links.set([]),
      });
  }
}
