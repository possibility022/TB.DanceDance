import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Params } from '@angular/router';

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
            <app-video-card
              [video]="video"
              [shareable]="shareable()"
              [transferable]="transferable()"
              [deletable]="deletable()"
              [addable]="addable()"
              [removable]="removable()"
              [queryParams]="queryParams()"
              [selected]="!!selectedBlobId() && video.blobId === selectedBlobId()"
              [badge]="badges().get(video.videoId ?? '') ?? ''"
              (share)="share.emit($event)"
              (transfer)="transfer.emit($event)"
              (deleteVideo)="deleteVideo.emit($event)"
              (add)="add.emit($event)"
              (remove)="remove.emit($event)"
            />
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
  readonly transferable = input(false);
  /** Show a per-card Delete action (owner-only). */
  readonly deletable = input(false);
  /** Show a per-card Add action (owner-only, e.g. recordings available to add to a competition). */
  readonly addable = input(false);
  /** Show a per-card Remove action (owner-only, e.g. detaching a recording from a competition). */
  readonly removable = input(false);
  /** Per-video badge text (e.g. flagging a recording already grouped into another competition), keyed by videoId. */
  readonly badges = input<ReadonlyMap<string, string>>(new Map());
  /** Scope carried to the player so it can show a sibling playlist. */
  readonly scopeGroupId = input<string>('');
  readonly scopeEventId = input<string>('');
  readonly scopeCompetitionId = input<string>('');
  /** Highlight the currently-playing recording (blob id). */
  readonly selectedBlobId = input<string>('');
  readonly share = output<VideoInformation>();
  readonly transfer = output<VideoInformation>();
  readonly deleteVideo = output<VideoInformation>();
  readonly add = output<VideoInformation>();
  readonly remove = output<VideoInformation>();

  readonly queryParams = computed<Params>(() => {
    if (this.scopeGroupId()) {
      return { groupId: this.scopeGroupId() };
    }
    if (this.scopeEventId()) {
      return { eventId: this.scopeEventId() };
    }
    if (this.scopeCompetitionId()) {
      return { competitionId: this.scopeCompetitionId() };
    }
    return {};
  });
}
