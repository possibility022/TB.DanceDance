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
              [deletable]="deletable()"
              [queryParams]="queryParams()"
              [selected]="!!selectedBlobId() && video.blobId === selectedBlobId()"
              [selectable]="selectable()"
              [checked]="!!video.videoId && selectedIds().includes(video.videoId)"
              (share)="share.emit($event)"
              (deleteVideo)="deleteVideo.emit($event)"
              (selectionToggle)="selectionToggle.emit($event)"
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
  /** Show a per-card Delete action (owner-only). */
  readonly deletable = input(false);
  /** Scope carried to the player so it can show a sibling playlist. */
  readonly scopeGroupId = input<string>('');
  readonly scopeEventId = input<string>('');
  /** Highlight the currently-playing recording (blob id). */
  readonly selectedBlobId = input<string>('');
  /** Show per-card selection checkboxes (multi-select mode). */
  readonly selectable = input(false);
  /** Video ids currently selected in multi-select mode. */
  readonly selectedIds = input<readonly string[]>([]);
  readonly share = output<VideoInformation>();
  readonly deleteVideo = output<VideoInformation>();
  readonly selectionToggle = output<VideoInformation>();

  readonly queryParams = computed<Params>(() => {
    if (this.scopeGroupId()) {
      return { groupId: this.scopeGroupId() };
    }
    if (this.scopeEventId()) {
      return { eventId: this.scopeEventId() };
    }
    return {};
  });
}
