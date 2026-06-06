import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DOCUMENT } from '@angular/common';

import { ConfigService } from '../../core/config/config.service';

const CONSENT_COOKIE = 'tbdancedanceappcookiesaccepted';
const ONE_YEAR_SECONDS = 60 * 60 * 24 * 365;

@Component({
  selector: 'app-cookie-consent',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (visible()) {
      <div class="notification is-info is-light cookie-consent" role="region" aria-label="Cookie consent">
        <div class="container is-flex is-align-items-center is-justify-content-space-between">
          <p class="mr-4">
            We use cookies to keep you signed in and remember your preferences. See our
            <a [href]="privacyUrl()" target="_blank" rel="noopener">privacy policy</a>.
          </p>
          <button type="button" class="button is-info" (click)="accept()">Accept</button>
        </div>
      </div>
    }
  `,
  styles: `
    .cookie-consent {
      position: fixed;
      inset: auto 0 0 0;
      border-radius: 0;
      margin: 0;
      z-index: 30;
    }
  `,
})
export class CookieConsent {
  private readonly doc = inject(DOCUMENT);
  private readonly config = inject(ConfigService);

  readonly visible = signal(!this.hasConsent());
  readonly privacyUrl = computed(() => `${this.config.config().authUrl}/policy/dancedanceapp`);

  accept(): void {
    this.doc.cookie = `${CONSENT_COOKIE}=true; path=/; max-age=${ONE_YEAR_SECONDS}; SameSite=Lax`;
    this.visible.set(false);
  }

  private hasConsent(): boolean {
    return this.doc.cookie
      .split('; ')
      .some((entry) => entry.startsWith(`${CONSENT_COOKIE}=`));
  }
}
