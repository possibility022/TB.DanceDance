import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { VideosService } from '../../core/api/videos.service';
import { VideoInformation } from '../../core/api/api-models';
import { VideoList } from '../../shared/ui/video-list/video-list';

@Component({
  selector: 'app-my-videos',
  imports: [VideoList],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './my-videos.html',
})
export class MyVideos {
  private readonly videos = inject(VideosService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly failed = signal(false);
  readonly items = signal<readonly VideoInformation[]>([]);

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.failed.set(false);

    this.videos
      .getMyVideos()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.items.set(response.videoInformation ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.failed.set(true);
          this.loading.set(false);
        },
      });
  }
}
