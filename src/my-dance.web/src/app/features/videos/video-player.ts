import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, forkJoin } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { CommentsService } from '../../core/api/comments.service';
import { VideosService } from '../../core/api/videos.service';
import { CommentResponse, VideoInformation } from '../../core/api/api-models';
import { CommentEdit, CommentReport, CommentsSection } from '../comments/comments-section';
import { LongDatePipe } from '../../shared/format/long-date.pipe';

@Component({
  selector: 'app-video-player',
  imports: [LongDatePipe, CommentsSection],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './video-player.html',
})
export class VideoPlayer implements OnInit {
  /** Bound from the route `:blobId` param via withComponentInputBinding(). */
  readonly blobId = input.required<string>();

  private readonly videos = inject(VideosService);
  private readonly comments = inject(CommentsService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly info = signal<VideoInformation | null>(null);
  readonly streamUrl = signal<string | null>(null);

  readonly commentList = signal<readonly CommentResponse[]>([]);
  private readonly videoId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
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

  private loadComments(videoId: string): void {
    this.comments
      .getCommentsForVideo(videoId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => this.commentList.set(response.comments ?? []),
        // Comments are non-critical; leave the list empty on failure.
        error: () => this.commentList.set([]),
      });
  }

  private afterMutation(action$: Observable<void>): void {
    action$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        const videoId = this.videoId();
        if (videoId) {
          this.loadComments(videoId);
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
