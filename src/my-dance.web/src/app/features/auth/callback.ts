import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

/**
 * Handles the sign-in redirect callback. `checkAuth()` already ran during app
 * initialization, so here we just route the user on (or offer a retry).
 */
@Component({
  selector: 'app-callback',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="has-text-centered py-6" aria-live="polite">
      @if (failed()) {
        <h1 class="title is-4">Sign-in failed</h1>
        <p class="block">Something went wrong while signing you in.</p>
        <button type="button" class="button is-primary" (click)="retry()">Try again</button>
      } @else {
        <p class="is-size-5">Signing you in…</p>
      }
    </div>
  `,
})
export class Callback {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly failed = signal(false);

  constructor() {
    if (this.auth.isAuthenticated()) {
      const returnUrl = this.auth.consumeReturnUrl() ?? '/';
      void this.router.navigateByUrl(returnUrl, { replaceUrl: true });
    } else {
      this.failed.set(true);
    }
  }

  retry(): void {
    this.failed.set(false);
    this.auth.login(this.auth.consumeReturnUrl() ?? '/');
  }
}
