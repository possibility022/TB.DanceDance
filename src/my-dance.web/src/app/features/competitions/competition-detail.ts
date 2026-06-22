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
import { DOCUMENT } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, forkJoin, switchMap } from 'rxjs';

import { CompetitionsService } from '../../core/api/competitions.service';
import { VideosService } from '../../core/api/videos.service';
import { CommentsService } from '../../core/api/comments.service';
import { CommentResponse, CompetitionResponse, VideoInformation } from '../../core/api/api-models';
import { ShareDialog } from '../sharing/share-dialog';
import { CommentEdit, CommentReport, CommentsSection } from '../comments/comments-section';
import { LongDatePipe } from '../../shared/format/long-date.pipe';
import { VideoList } from '../../shared/ui/video-list/video-list';
import { AddVideosDialog } from './add-videos-dialog';

const PAGE_SIZE = 100;
const COMMENTS_PAGE_SIZE = 20;

/** A single competition: rename, delete, share, and manage its grouped recordings. */
@Component({
  selector: 'app-competition-detail',
  imports: [ShareDialog, CommentsSection, RouterLink, LongDatePipe, VideoList, AddVideosDialog],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './competition-detail.html',
})
export class CompetitionDetail {
  /** Route param (bound via withComponentInputBinding). */
  readonly competitionId = input<string>('');

  private readonly competitions = inject(CompetitionsService);
  private readonly videos = inject(VideosService);
  private readonly comments = inject(CommentsService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly doc = inject(DOCUMENT);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly competition = signal<CompetitionResponse | null>(null);

  /** The owner's recordings available to add (those not already in this competition). */
  readonly myVideos = signal<readonly VideoInformation[]>([]);
  readonly addError = signal<string | null>(null);
  readonly addDialogOpen = signal(false);
  readonly shareOpen = signal(false);

  /** The other competition (id + name) a recording is already grouped into, keyed by videoId. */
  readonly otherCompetitionMemberships = signal<ReadonlyMap<string, { id: string; name: string }>>(
    new Map(),
  );
  readonly otherCompetitionBadges = computed(() => {
    const badges = new Map<string, string>();
    for (const [videoId, { name }] of this.otherCompetitionMemberships()) {
      badges.set(videoId, `Also in ${name}`);
    }
    return badges;
  });

  readonly commentList = signal<readonly CommentResponse[]>([]);
  readonly loadingMoreComments = signal(false);
  readonly canLoadMoreComments = signal(false);
  private currentCommentsPage = 0;

  readonly groupedVideos = computed(() => this.competition()?.videos ?? []);
  readonly addableVideos = computed(() => {
    const grouped = new Set(this.groupedVideos().map((v) => v.videoId));
    return this.myVideos().filter((v) => !grouped.has(v.videoId));
  });

  constructor() {
    effect(() => {
      const id = this.competitionId();
      if (id) {
        this.load(id);
      }
    });
  }

  private load(id: string): void {
    this.loading.set(true);
    this.failed.set(false);
    this.competitions
      .getCompetition(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (competition) => {
          this.competition.set(competition);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });

    this.videos
      .getMyVideos(1, PAGE_SIZE)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => this.myVideos.set(response.items ?? []),
        error: () => this.myVideos.set([]),
      });

    this.loadComments(id);
    this.loadOtherCompetitionMemberships(id);
  }

  /** Tracks recordings already grouped into a different competition, so they can be moved over with a warning. */
  private loadOtherCompetitionMemberships(currentCompetitionId: string): void {
    this.competitions
      .getMyCompetitions()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const others = (response.competitions ?? []).filter(
            (c) => c.id && c.id !== currentCompetitionId && (c.videoCount ?? 0) > 0,
          );
          if (others.length === 0) {
            this.otherCompetitionMemberships.set(new Map());
            return;
          }
          forkJoin(others.map((c) => this.competitions.getCompetition(c.id!)))
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (details) => {
                const memberships = new Map<string, { id: string; name: string }>();
                for (const detail of details) {
                  if (!detail.id || !detail.name) {
                    continue;
                  }
                  for (const video of detail.videos ?? []) {
                    if (video.videoId) {
                      memberships.set(video.videoId, { id: detail.id, name: detail.name });
                    }
                  }
                }
                this.otherCompetitionMemberships.set(memberships);
              },
              error: () => this.otherCompetitionMemberships.set(new Map()),
            });
        },
        error: () => this.otherCompetitionMemberships.set(new Map()),
      });
  }

  private loadComments(competitionId: string): void {
    this.comments
      .getCommentsForCompetition(competitionId, 1, COMMENTS_PAGE_SIZE)
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
    const competitionId = this.competitionId();
    if (!competitionId || this.loadingMoreComments() || !this.canLoadMoreComments()) {
      return;
    }

    this.loadingMoreComments.set(true);
    const nextPage = this.currentCommentsPage + 1;

    this.comments
      .getCommentsForCompetition(competitionId, nextPage, COMMENTS_PAGE_SIZE)
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
    const competitionId = this.competitionId();
    if (!competitionId) {
      return;
    }
    const pageSize = this.currentCommentsPage * COMMENTS_PAGE_SIZE;

    this.comments
      .getCommentsForCompetition(competitionId, 1, pageSize)
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

  private afterCommentMutation(action$: Observable<void>): void {
    action$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.reloadLoadedComments(),
    });
  }

  onSaveEdit({ commentId, content }: CommentEdit): void {
    this.afterCommentMutation(this.comments.updateComment(commentId, { content }));
  }

  onRemoveComment(commentId: string): void {
    this.afterCommentMutation(this.comments.deleteComment(commentId));
  }

  onHideComment(commentId: string): void {
    this.afterCommentMutation(this.comments.hideComment(commentId));
  }

  onUnhideComment(commentId: string): void {
    this.afterCommentMutation(this.comments.unhideComment(commentId));
  }

  onReportComment({ commentId, reason }: CommentReport): void {
    this.afterCommentMutation(this.comments.reportComment(commentId, { reason }));
  }

  rename(): void {
    const current = this.competition();
    if (!current?.id) {
      return;
    }
    const newName = this.doc.defaultView?.prompt('Rename competition', current.name ?? '')?.trim();
    if (!newName || newName === current.name) {
      return;
    }
    this.competitions
      .renameCompetition(current.id, { newName })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.competition.update((c) => (c ? { ...c, name: newName } : c)),
      });
  }

  remove(video: VideoInformation): void {
    const competitionId = this.competition()?.id;
    if (!competitionId || !video.videoId) {
      return;
    }
    this.competitions
      .removeVideo(competitionId, video.videoId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () =>
          this.competition.update((c) =>
            c ? { ...c, videos: (c.videos ?? []).filter((v) => v.videoId !== video.videoId) } : c,
          ),
      });
  }

  add(video: VideoInformation): void {
    const competitionId = this.competition()?.id;
    if (!competitionId || !video.videoId) {
      return;
    }
    const videoId = video.videoId;
    const otherCompetition = this.otherCompetitionMemberships().get(videoId);
    if (otherCompetition) {
      const confirmed = this.doc.defaultView?.confirm(
        `“${video.name}” is already in “${otherCompetition.name}”. Move it here too? It will be removed from “${otherCompetition.name}”.`,
      );
      if (!confirmed) {
        return;
      }
    }
    this.addError.set(null);
    const add$ = otherCompetition
      ? this.competitions
          .removeVideo(otherCompetition.id, videoId)
          .pipe(switchMap(() => this.competitions.addVideo(competitionId, videoId)))
      : this.competitions.addVideo(competitionId, videoId);
    add$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.competition.update((c) => (c ? { ...c, videos: [...(c.videos ?? []), video] } : c));
        if (otherCompetition) {
          this.otherCompetitionMemberships.update((map) => {
            const next = new Map(map);
            next.delete(videoId);
            return next;
          });
        }
      },
      error: () =>
        this.addError.set(
          `Couldn’t add “${video.name}”. It may already belong to another competition.`,
        ),
    });
  }

  deleteCompetition(): void {
    const current = this.competition();
    if (!current?.id) {
      return;
    }
    const confirmed = this.doc.defaultView?.confirm(
      `Delete “${current.name}”? Its recordings stay in your library but are no longer grouped, and its shared links stop working.`,
    );
    if (!confirmed) {
      return;
    }
    this.competitions
      .deleteCompetition(current.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => void this.router.navigate(['/competitions']),
      });
  }

  openShare(): void {
    this.shareOpen.set(true);
  }

  closeShare(): void {
    this.shareOpen.set(false);
  }

  openAddDialog(): void {
    this.addError.set(null);
    this.addDialogOpen.set(true);
  }

  closeAddDialog(): void {
    this.addDialogOpen.set(false);
  }
}
