import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Silent-renew route. With refresh tokens, renewal happens via the token
 * endpoint rather than a hidden iframe, so this route exists only to satisfy the
 * registered silent-renew URL and renders nothing meaningful.
 */
@Component({
  selector: 'app-silent-renew',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: ``,
})
export class SilentRenew {}
