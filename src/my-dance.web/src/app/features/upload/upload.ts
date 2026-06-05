import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-upload',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="title">Upload a recording</h1>
    <p class="subtitle">Add a new recording to a group, event, or your private library.</p>
    <div class="notification is-light">Coming soon.</div>
  `,
})
export class Upload {}
