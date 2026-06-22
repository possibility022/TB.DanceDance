import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { VideoInformation } from '../../core/api/api-models';
import { VideoList } from '../../shared/ui/video-list/video-list';

/** Modal: pick recordings to add to a competition. Stays open across multiple adds. */
@Component({
  selector: 'app-add-videos-dialog',
  imports: [VideoList],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './add-videos-dialog.html',
})
export class AddVideosDialog {
  readonly open = input(false);
  readonly videos = input<readonly VideoInformation[]>([]);
  /** Per-video warning badge (e.g. already grouped into another competition), keyed by videoId. */
  readonly badges = input<ReadonlyMap<string, string>>(new Map());
  readonly error = input<string | null>(null);
  readonly add = output<VideoInformation>();
  readonly closed = output<void>();

  close(): void {
    this.closed.emit();
  }
}
