import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-my-videos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="title">My recordings</h1>
    <p class="subtitle">Your private library and shareable links.</p>
    <div class="notification is-light">Coming soon.</div>
  `,
})
export class MyVideos {}
