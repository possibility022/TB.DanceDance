import { CanActivateFn } from '@angular/router';

/**
 * Admin-only routes. For now requires authentication (handled separately by
 * `autoLoginPartialRoutesGuard` on the route); admin capability is
 * backend-driven and will be enforced once the API schema is wired in.
 */
export const adminGuard: CanActivateFn = (_route, _state) => {
  // TODO: gate on the backend-provided admin capability flag (see doc 08).
  return true;
};
