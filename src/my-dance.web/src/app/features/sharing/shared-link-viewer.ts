import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Public viewer for a single shared recording. Reachable at a stable URL. */
@Component({
  selector: 'app-shared-link-viewer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h1 class="title">Shared recording</h1>
    <p class="subtitle">Viewing shared link <code>{{ linkId() }}</code>.</p>
    <div class="notification is-light">Coming soon.</div>
  `,
})
export class SharedLinkViewer {
  /** Bound from the route `:linkId` param via withComponentInputBinding(). */
  readonly linkId = input.required<string>();
}
