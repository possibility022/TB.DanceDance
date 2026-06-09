import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ViewportScroller } from '@angular/common';
import { Observable, forkJoin } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { CommentsService } from '../../core/api/comments.service';
import { GroupsService } from '../../core/api/groups.service';
import { EventsService } from '../../core/api/events.service';
import { VideosService } from '../../core/api/videos.service';
import { CommentResponse, VideoInformation } from '../../core/api/api-models';
import { CommentEdit, CommentReport, CommentsSection } from '../comments/comments-section';
import { LongDatePipe } from '../../shared/format/long-date.pipe';
import { VideoList } from '../../shared/ui/video-list/video-list';

export type SidebarTab = 'comments' | 'recordings';

const COMMENTS_PAGE_SIZE = 20;
const SIBLINGS_PAGE_SIZE = 100;

@Component({
  selector: 'app-video-player',
  imports: [LongDatePipe, CommentsSection, VideoList],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './video-player.html',
})
export class VideoPlayer {
  /** Bound from the route `:blobId` param via withComponentInputBinding(). */
  readonly blobId = input.required<string>();
  /** Optional scope (query params) — drives the sibling "playlist". */
  readonly groupId = input<string>('');
  readonly eventId = input<string>('');

  private readonly videos = inject(VideosService);
  private readonly groups = inject(GroupsService);
  private readonly events = inject(EventsService);
  private readonly comments = inject(CommentsService);
  private readonly auth = inject(AuthService);
  private readonly viewport = inject(ViewportScroller);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly info = signal<VideoInformation | null>(null);
  readonly streamUrl = signal<string | null>(null);

  readonly commentList = signal<readonly CommentResponse[]>([]);
  readonly loadingMoreComments = signal(false);
  readonly canLoadMoreComments = signal(false);
  private currentCommentsPage = 0;
  private readonly videoId = signal<string | null>(null);

  readonly siblings = signal<readonly VideoInformation[]>([]);

  readonly editingName = signal(false);
  readonly nameDraft = signal('');
  readonly renaming = signal(false);

  /** User-selected tab. Reads through `activeTab` to guard against stale `recordings` state when no siblings are loaded. */
  private readonly tabChoice = signal<SidebarTab>('recordings');
  readonly activeTab = computed<SidebarTab>(() =>
    this.tabChoice() === 'recordings' && this.siblings().length === 0 ? 'comments' : this.tabChoice(),
  );

  setTab(tab: SidebarTab): void {
    this.tabChoice.set(tab);
  }

  constructor() {
    // Reload when the recording changes (incl. navigating between siblings,
    // which reuses this component instance).
    effect(() => {
      this.blobId();
      this.load();
      // The component is reused when switching between siblings, so the router
      // doesn't reset scroll. Bring the player back into view at the top.
      this.viewport.scrollToPosition([0, 0]);
    });
    effect(() => {
      this.groupId();
      this.eventId();
      this.loadSiblings();
    });
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);
    this.streamUrl.set(null);

    forkJoin({
      info: this.videos.getVideo(this.blobId()),
      token: this.auth.getAccessToken(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ info, token }) => {
          const video = info.videoInformation ?? null;
          this.info.set(video);
          if (video?.converted && video.blobId) {
            this.streamUrl.set(this.videos.streamUrl(video.blobId, token));
          }
          this.loading.set(false);

          const videoId = video?.videoId ?? null;
          this.videoId.set(videoId);
          if (videoId) {
            this.loadComments(videoId);
          }
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }

  private loadSiblings(): void {
    const request$ = this.groupId()
      ? this.groups.getVideosForGroup(this.groupId(), 1, SIBLINGS_PAGE_SIZE)
      : this.eventId()
        ? this.events.getEventVideos(this.eventId(), 1, SIBLINGS_PAGE_SIZE)
        : null;
    if (!request$) {
      this.siblings.set([]);
      return;
    }
    request$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response) => this.siblings.set(response.items ?? []),
      error: () => this.siblings.set([]),
    });
  }

  startRename(): void {
    this.nameDraft.set(this.info()?.name ?? '');
    this.editingName.set(true);
  }

  cancelRename(): void {
    this.editingName.set(false);
  }

  saveRename(): void {
    const videoId = this.info()?.videoId;
    const name = this.nameDraft().trim();
    if (!videoId || !name || this.renaming()) {
      return;
    }
    this.renaming.set(true);
    this.videos
      .renameVideo(videoId, { newName: name })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.info.update((video) => (video ? { ...video, name } : video));
          this.renaming.set(false);
          this.editingName.set(false);
        },
        error: () => this.renaming.set(false),
      });
  }

  private loadComments(videoId: string): void {
    this.comments
      .getCommentsForVideo(videoId, 1, COMMENTS_PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const items = response.items ?? [];
          this.commentList.set(items);
          this.currentCommentsPage = 1;
          this.canLoadMoreComments.set(items.length < (response.totalCount ?? 0));
        },
        // Comments are non-critical; leave the list empty on failure.
        error: () => {
          this.commentList.set([]);
          this.canLoadMoreComments.set(false);
        },
      });
  }

  loadMoreComments(): void {
    const videoId = this.videoId();
    if (!videoId || this.loadingMoreComments() || !this.canLoadMoreComments()) {
      return;
    }

    this.loadingMoreComments.set(true);
    const nextPage = this.currentCommentsPage + 1;

    this.comments
      .getCommentsForVideo(videoId, nextPage, COMMENTS_PAGE_SIZE)
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
  private reloadLoadedComments(videoId: string): void {
    const pageSize = this.currentCommentsPage * COMMENTS_PAGE_SIZE;

    this.comments
      .getCommentsForVideo(videoId, 1, pageSize)
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

  private afterMutation(action$: Observable<void>): void {
    action$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        const videoId = this.videoId();
        if (videoId) {
          this.reloadLoadedComments(videoId);
        }
      },
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
