import { Injectable, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { Observable } from 'rxjs';

const RETURN_URL_KEY = 'auth.returnUrl';

/**
 * Thin wrapper over {@link OidcSecurityService} exposing auth state as signals
 * and the few actions the app needs. The backend remains the source of truth
 * for capabilities; this only reflects "is there a valid session".
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oidc = inject(OidcSecurityService);

  private readonly authState = toSignal(this.oidc.isAuthenticated$, {
    initialValue: { isAuthenticated: false, allConfigsAuthenticated: [] },
  });
  private readonly user = toSignal(this.oidc.userData$, {
    initialValue: { userData: null, allUserData: [] },
  });

  readonly isAuthenticated = computed(() => this.authState().isAuthenticated);
  readonly userData = computed(() => this.user().userData);

  /** Processes the sign-in redirect / hydrates the session from storage. */
  checkAuth(): Observable<unknown> {
    return this.oidc.checkAuth();
  }

  /** Starts the OIDC login flow, remembering where to return afterwards. */
  login(returnUrl?: string): void {
    if (returnUrl) {
      sessionStorage.setItem(RETURN_URL_KEY, returnUrl);
    }
    this.oidc.authorize();
  }

  /** Ends the session (calls the OIDC end-session endpoint). */
  logout(): Observable<unknown> {
    return this.oidc.logoff();
  }

  getAccessToken(): Observable<string> {
    return this.oidc.getAccessToken();
  }

  /** Reads and clears the stored post-login return URL. */
  consumeReturnUrl(): string | null {
    const url = sessionStorage.getItem(RETURN_URL_KEY);
    if (url) {
      sessionStorage.removeItem(RETURN_URL_KEY);
    }
    return url;
  }
}
