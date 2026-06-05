import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-video-player',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="title">Watch recording</h1>
    <p class="subtitle">Player and comments for recording <code>{{ videoId() }}</code>.</p>
    <div class="notification is-light">Coming soon.</div>
  `,
})
export class VideoPlayer {
  /** Bound from the route `:videoId` param via withComponentInputBinding(). */
  readonly videoId = input.required<string>();
}
