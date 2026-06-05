import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { VideoInformation } from '../../../core/api/api-models';
import { VideoCard } from '../video-card/video-card';

/** Responsive grid of recordings, with an empty-state message. */
@Component({
  selector: 'app-video-list',
  imports: [VideoCard],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (videos().length === 0) {
      <p class="has-text-grey">{{ emptyMessage() }}</p>
    } @else {
      <div class="columns is-multiline">
        @for (video of videos(); track video.videoId ?? video.blobId ?? $index) {
          <div class="column is-one-third-desktop is-half-tablet">
            <app-video-card [video]="video" [shareable]="shareable()" (share)="share.emit($event)" />
          </div>
        }
      </div>
    }
  `,
})
export class VideoList {
  readonly videos = input.required<readonly VideoInformation[]>();
  readonly emptyMessage = input('No recordings yet.');
  readonly shareable = input(false);
  readonly share = output<VideoInformation>();
}
