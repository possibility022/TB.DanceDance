import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';

import { AuthService } from './auth.service';

/**
 * Allows authenticated users through; otherwise starts the OIDC login flow and
 * remembers the requested URL so the callback can return the user there.
 *
 * Relies on `checkAuth()` having run during app initialization, so the auth
 * state signal is already settled by the time any guard evaluates.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.isAuthenticated()) {
    return true;
  }
  auth.login(state.url);
  return false;
};

/**
 * Admin-only routes. For now requires authentication; admin capability is
 * backend-driven and will be enforced once the API schema is wired in.
 */
export const adminGuard: CanActivateFn = (route, state) => {
  // TODO: gate on the backend-provided admin capability flag (see doc 08).
  return authGuard(route, state);
};
