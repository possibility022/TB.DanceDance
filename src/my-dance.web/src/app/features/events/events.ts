import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-events',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="title">Events</h1>
    <p class="subtitle">Workshops, competitions and showcases — and their recordings.</p>
    <div class="notification is-light">Coming soon.</div>
  `,
})
export class Events {}
