import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { AnonymousIdService } from '../../core/anonymous-id.service';
import { CommentsService } from '../../core/api/comments.service';
import { SharingService } from '../../core/api/sharing.service';
import { CommentResponse, SharedVideoInfoResponse } from '../../core/api/api-models';
import { CommentDraft, CommentEdit, CommentReport, CommentsSection } from '../comments/comments-section';
import { LongDatePipe } from '../../shared/format/long-date.pipe';

/** Public viewer for one shared recording, reachable at a stable URL. */
@Component({
  selector: 'app-shared-link-viewer',
  imports: [LongDatePipe, CommentsSection],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shared-link-viewer.html',
})
export class SharedLinkViewer implements OnInit {
  /** Bound from the route `:linkId` param via withComponentInputBinding(). */
  readonly linkId = input.required<string>();

  private readonly sharing = inject(SharingService);
  private readonly comments = inject(CommentsService);
  private readonly auth = inject(AuthService);
  private readonly anonymousId = inject(AnonymousIdService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly commentsRef = viewChild(CommentsSection);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly info = signal<SharedVideoInfoResponse | null>(null);
  readonly streamUrl = signal<string | null>(null);
  readonly commentList = signal<readonly CommentResponse[]>([]);
  readonly submitting = signal(false);

  readonly canCompose = computed(() => {
    const info = this.info();
    if (!info?.allowCommentsOnThisLink) {
      return false;
    }
    return this.isAuthenticated() || !!info.allowAnonymousCommentsOnThisLink;
  });

  readonly requireSignature = computed(() => !this.isAuthenticated());

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.sharing
      .getSharedVideo(this.linkId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (info) => {
          this.info.set(info);
          this.streamUrl.set(this.sharing.sharedStreamUrl(this.linkId()));
          this.loading.set(false);
          this.loadComments();
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }

  private loadComments(): void {
    this.comments
      .getCommentsByLink(this.linkId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => this.commentList.set(response.comments ?? []),
        error: () => this.commentList.set([]),
      });
  }

  onCreate(draft: CommentDraft): void {
    this.submitting.set(true);
    this.comments
      .addCommentByLink(this.linkId(), {
        content: draft.content,
        authorName: draft.authorName,
        anonymousId: this.isAuthenticated() ? undefined : this.anonymousId.getId(),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.commentsRef()?.resetComposer();
          this.loadComments();
        },
        error: () => this.submitting.set(false),
      });
  }

  private afterMutation(action$: Observable<void>): void {
    action$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.loadComments(),
    });
  }

  onSaveEdit({ commentId, content }: CommentEdit): void {
    this.afterMutation(this.comments.updateComment(commentId, { content }));
  }

  onRemove(commentId: string): void {
    this.afterMutation(this.comments.deleteComment(commentId));
  }

  onHide(commentId: string): void {
    this.afterMutation(this.comments.hideComment(commentId));
  }

  onUnhide(commentId: string): void {
    this.afterMutation(this.comments.unhideComment(commentId));
  }

  onReport({ commentId, reason }: CommentReport): void {
    this.afterMutation(this.comments.reportComment(commentId, { reason }));
  }
}
