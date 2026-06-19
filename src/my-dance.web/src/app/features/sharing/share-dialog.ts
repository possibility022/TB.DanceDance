import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  linkedSignal,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { SharingService } from '../../core/api/sharing.service';
import { CompetitionsService } from '../../core/api/competitions.service';
import { VideosService } from '../../core/api/videos.service';
import { SharedLinkResponse } from '../../core/api/api-models';
import { CommentVisibility, COMMENT_VISIBILITY_LABELS } from '../../shared/format/enums';
import { LongDatePipe } from '../../shared/format/long-date.pipe';

const DEFAULT_EXPIRATION_DAYS = 7;

const VISIBILITY_OPTIONS = [
  CommentVisibility.LoggedInOnly,
  CommentVisibility.OwnerOnly,
  CommentVisibility.Everyone,
].map((value) => ({ value, label: COMMENT_VISIBILITY_LABELS[value] }));

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
  readonly commentVisibility = input<number>(0);
  /** When set, the dialog shares a whole competition instead of a single recording. */
  readonly competitionId = input<string>('');
  readonly open = input(false);
  readonly closed = output<void>();

  private readonly sharing = inject(SharingService);
  private readonly competitions = inject(CompetitionsService);
  private readonly videos = inject(VideosService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly visibilityOptions = VISIBILITY_OPTIONS;

  /** True when this dialog targets a competition rather than a single recording. */
  readonly isCompetition = computed(() => !!this.competitionId());

  readonly form = this.fb.nonNullable.group({
    expirationDays: [DEFAULT_EXPIRATION_DAYS, [Validators.required, Validators.min(1), Validators.max(365)]],
    allowComments: [true],
    allowAnonymousComments: [false],
  });

  readonly links = signal<readonly SharedLinkResponse[]>([]);
  readonly creating = signal(false);
  readonly copiedLinkId = signal<string | null>(null);

  /** Current saved visibility, and the (possibly changed) selection. */
  readonly savedVisibility = linkedSignal(() => this.commentVisibility());
  readonly selectedVisibility = linkedSignal(() => this.commentVisibility());
  readonly updatingVisibility = signal(false);

  constructor() {
    effect(() => {
      if (this.open() && (this.videoId() || this.competitionId())) {
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
    const request = this.form.getRawValue();
    const create$ = this.isCompetition()
      ? this.competitions.createSharedLink(this.competitionId(), request)
      : this.sharing.createSharedLink(this.videoId(), request);
    create$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.creating.set(false);
        this.loadLinks();
      },
      error: () => this.creating.set(false),
    });
  }

  applyVisibility(): void {
    if (this.updatingVisibility() || this.selectedVisibility() === this.savedVisibility()) {
      return;
    }
    const visibility = this.selectedVisibility();
    this.updatingVisibility.set(true);
    this.videos
      .updateCommentSettings(this.videoId(), { commentVisibility: visibility })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.savedVisibility.set(visibility);
          this.updatingVisibility.set(false);
        },
        error: () => this.updatingVisibility.set(false),
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
          this.links.set(
            (response.links ?? []).filter((link) =>
              this.isCompetition()
                ? link.competitionId === this.competitionId()
                : link.videoId === this.videoId(),
            ),
          ),
        error: () => this.links.set([]),
      });
  }
}
