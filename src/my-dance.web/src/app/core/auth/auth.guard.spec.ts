import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { adminGuard } from './auth.guard';

function runGuard(guard: typeof adminGuard, url: string) {
  const state = { url } as RouterStateSnapshot;
  const route = {} as ActivatedRouteSnapshot;
  return TestBed.runInInjectionContext(() => guard(route, state));
}

describe('adminGuard', () => {
  it('allows the route through (auth itself is handled by autoLoginPartialRoutesGuard)', () => {
    expect(runGuard(adminGuard, '/access/requestedaccesses')).toBe(true);
  });
});
