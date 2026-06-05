import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-group-videos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="title">Lesson recordings</h1>
    <p class="subtitle">Browse recordings from your regular groups.</p>
    <div class="notification is-light">Coming soon.</div>
  `,
})
export class GroupVideos {}
