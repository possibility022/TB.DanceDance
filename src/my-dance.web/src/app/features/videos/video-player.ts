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
import { forkJoin } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { VideosService } from '../../core/api/videos.service';
import { VideoInformation } from '../../core/api/api-models';
import { LongDatePipe } from '../../shared/format/long-date.pipe';

@Component({
  selector: 'app-video-player',
  imports: [LongDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './video-player.html',
})
export class VideoPlayer implements OnInit {
  /** Bound from the route `:blobId` param via withComponentInputBinding(). */
  readonly blobId = input.required<string>();

  private readonly videos = inject(VideosService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly info = signal<VideoInformation | null>(null);
  readonly streamUrl = signal<string | null>(null);

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
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }
}
