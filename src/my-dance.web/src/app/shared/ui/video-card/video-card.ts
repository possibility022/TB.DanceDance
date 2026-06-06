import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Params, RouterLink } from '@angular/router';

import { VideoInformation } from '../../../core/api/api-models';
import { LongDatePipe } from '../../format/long-date.pipe';

/** A single recording: name, recorded date, duration, and actions. */
@Component({
  selector: 'app-video-card',
  imports: [RouterLink, LongDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="card" [class.has-background-link-light]="selected()">
      <div class="card-content">
        @if (badge()) {
          <span class="tag is-info is-light mb-2">{{ badge() }}</span>
        }
        <h3 class="title is-6 mb-1">{{ video().name }}</h3>
        <p class="is-size-7 has-text-grey">
          {{ video().recordedDateTime | longDate }}
          @if (video().duration) {
            · {{ video().duration }}
          }
        </p>

        <div class="buttons are-small mt-3">
          @if (video().converted && video().blobId) {
            <a class="button is-primary" [routerLink]="['/videos', video().blobId]" [queryParams]="queryParams()">
              Watch
            </a>
          } @else {
            <span class="tag is-warning is-light">Processing…</span>
          }
          @if (shareable()) {
            <button type="button" class="button is-light" (click)="share.emit(video())">Share</button>
          }
        </div>
      </div>
    </div>
  `,
})
export class VideoCard {
  readonly video = input.required<VideoInformation>();
  /** Show the Share action (e.g. in the user's own library). */
  readonly shareable = input(false);
  /** Query params carried to the player to preserve the group/event scope. */
  readonly queryParams = input<Params>({});
  /** Highlight this card as the currently-playing recording. */
  readonly selected = input(false);
  /** Optional contextual label shown above the title (e.g. group name). */
  readonly badge = input('');
  readonly share = output<VideoInformation>();
}
