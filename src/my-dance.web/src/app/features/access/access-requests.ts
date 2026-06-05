import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-access-requests',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="title">Manage access requests</h1>
    <p class="subtitle">Approve or reject pending access requests.</p>
    <div class="notification is-light">Coming soon.</div>
  `,
})
export class AccessRequests {}
