import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';

/**
 * Handles the post-logout redirect. The OIDC library clears local auth state on
 * the end-session round-trip; here we just return the user home.
 */
@Component({
  selector: 'app-logout-callback',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<p class="has-text-centered py-6 is-size-5" aria-live="polite">Signing out…</p>`,
})
export class LogoutCallback {
  private readonly router = inject(Router);

  constructor() {
    void this.router.navigateByUrl('/', { replaceUrl: true });
  }
}
