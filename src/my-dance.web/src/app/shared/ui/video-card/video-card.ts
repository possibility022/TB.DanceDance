import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { VideoInformation } from '../../../core/api/api-models';
import { LongDatePipe } from '../../format/long-date.pipe';

/** A single recording: name, recorded date, duration, and a watch action. */
@Component({
  selector: 'app-video-card',
  imports: [RouterLink, LongDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="card">
      <div class="card-content">
        <h3 class="title is-6 mb-1">{{ video().name }}</h3>
        <p class="is-size-7 has-text-grey">
          {{ video().recordedDateTime | longDate }}
          @if (video().duration) {
            · {{ video().duration }}
          }
        </p>

        @if (video().converted && video().blobId) {
          <a class="button is-small is-primary mt-3" [routerLink]="['/videos', video().blobId]">
            Watch
          </a>
        } @else {
          <span class="tag is-warning is-light mt-3">Processing…</span>
        }
      </div>
    </div>
  `,
})
export class VideoCard {
  readonly video = input.required<VideoInformation>();
}
