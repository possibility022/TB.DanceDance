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
import { CommentResponse, SharedVideoInfoResponse, SharedVideoItem } from '../../core/api/api-models';
import { CommentDraft, CommentEdit, CommentReport, CommentsSection } from '../comments/comments-section';
import { LongDatePipe } from '../../shared/format/long-date.pipe';

const COMMENTS_PAGE_SIZE = 20;

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
  readonly loadingMoreComments = signal(false);
  readonly canLoadMoreComments = signal(false);
  private currentCommentsPage = 0;
  readonly submitting = signal(false);

  /** True when the link targets a whole competition (multiple videos, one combined thread). */
  readonly isCompetition = computed(() => !!this.info()?.isCompetition);

  /** The competition's videos paired with their per-video stream URL under this link. */
  readonly competitionVideos = computed<readonly (SharedVideoItem & { url: string })[]>(() => {
    const info = this.info();
    if (!info?.isCompetition) {
      return [];
    }
    return (info.videos ?? [])
      .filter((v): v is SharedVideoItem & { videoId: string } => !!v.videoId)
      .map((v) => ({ ...v, url: this.sharing.sharedVideoStreamUrl(this.linkId(), v.videoId) }));
  });

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
      .getCommentsByLink(this.linkId(), 1, COMMENTS_PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const items = response.items ?? [];
          this.commentList.set(items);
          this.currentCommentsPage = 1;
          this.canLoadMoreComments.set(items.length < (response.totalCount ?? 0));
        },
        error: () => {
          this.commentList.set([]);
          this.canLoadMoreComments.set(false);
        },
      });
  }

  loadMoreComments(): void {
    if (this.loadingMoreComments() || !this.canLoadMoreComments()) {
      return;
    }

    this.loadingMoreComments.set(true);
    const nextPage = this.currentCommentsPage + 1;

    this.comments
      .getCommentsByLink(this.linkId(), nextPage, COMMENTS_PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const items = [...this.commentList(), ...(response.items ?? [])];
          this.commentList.set(items);
          this.currentCommentsPage = nextPage;
          this.canLoadMoreComments.set(items.length < (response.totalCount ?? 0));
          this.loadingMoreComments.set(false);
        },
        error: () => this.loadingMoreComments.set(false),
      });
  }

  /** Refetches everything currently shown (rather than collapsing back to page 1) so a mutation doesn't hide already-loaded comments. */
  private reloadLoadedComments(): void {
    const pageSize = this.currentCommentsPage * COMMENTS_PAGE_SIZE;

    this.comments
      .getCommentsByLink(this.linkId(), 1, pageSize)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const items = response.items ?? [];
          this.commentList.set(items);
          this.canLoadMoreComments.set(items.length < (response.totalCount ?? 0));
        },
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
          this.reloadLoadedComments();
        },
        error: () => this.submitting.set(false),
      });
  }

  private afterMutation(action$: Observable<void>): void {
    action$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.reloadLoadedComments(),
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
