import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { AuthService } from '../../core/auth/auth.service';

/** Triggers logout (end-session with the id-token hint). */
@Component({
  selector: 'app-logout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<p class="has-text-centered py-6 is-size-5" aria-live="polite">Signing you out…</p>`,
})
export class Logout {
  private readonly auth = inject(AuthService);

  constructor() {
    this.auth.logout().subscribe();
  }
}
