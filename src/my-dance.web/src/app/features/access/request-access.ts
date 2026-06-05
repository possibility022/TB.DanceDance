import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-request-access',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="title">Request access</h1>
    <p class="subtitle">Ask to be granted access to groups and events.</p>
    <div class="notification is-light">Coming soon.</div>
  `,
})
export class RequestAccess {}
