import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { signal } from '@angular/core';

import { adminGuard, authGuard } from './auth.guard';
import { AuthService } from './auth.service';

function runGuard(guard: typeof authGuard, url: string) {
  const state = { url } as RouterStateSnapshot;
  const route = {} as ActivatedRouteSnapshot;
  return TestBed.runInInjectionContext(() => guard(route, state));
}

describe('authGuard', () => {
  const authed = signal(false);
  const login = vi.fn();

  beforeEach(() => {
    authed.set(false);
    login.mockClear();
    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: { isAuthenticated: authed, login } }],
    });
  });

  it('allows authenticated users through without logging in', () => {
    authed.set(true);

    expect(runGuard(authGuard, '/videos')).toBe(true);
    expect(login).not.toHaveBeenCalled();
  });

  it('blocks anonymous users and starts the login flow with the requested url', () => {
    expect(runGuard(authGuard, '/videos/my')).toBe(false);
    expect(login).toHaveBeenCalledWith('/videos/my');
  });
});

describe('adminGuard', () => {
  const authed = signal(false);
  const login = vi.fn();

  beforeEach(() => {
    authed.set(false);
    login.mockClear();
    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: { isAuthenticated: authed, login } }],
    });
  });

  it('delegates to the auth check (allows authenticated users for now)', () => {
    authed.set(true);
    expect(runGuard(adminGuard, '/access/requestedaccesses')).toBe(true);
  });

  it('blocks anonymous users and remembers the requested url', () => {
    expect(runGuard(adminGuard, '/access/requestedaccesses')).toBe(false);
    expect(login).toHaveBeenCalledWith('/access/requestedaccesses');
  });
});
