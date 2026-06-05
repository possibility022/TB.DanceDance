import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { signal } from '@angular/core';

import { Callback } from './callback';
import { AuthService } from '../../core/auth/auth.service';

function setup(opts: { authed: boolean; returnUrl?: string | null }) {
  const auth = {
    isAuthenticated: signal(opts.authed),
    consumeReturnUrl: vi.fn(() => opts.returnUrl ?? null),
    login: vi.fn(),
  };
  const router = { navigateByUrl: vi.fn() };

  TestBed.configureTestingModule({
    imports: [Callback],
    providers: [
      { provide: AuthService, useValue: auth },
      { provide: Router, useValue: router },
    ],
  });

  const fixture = TestBed.createComponent(Callback);
  fixture.detectChanges();
  return { fixture, auth, router, component: fixture.componentInstance };
}

describe('Callback', () => {
  it('redirects to the stored return url after a successful sign-in', () => {
    const { component, router } = setup({ authed: true, returnUrl: '/videos/my' });

    expect(component.failed()).toBe(false);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/videos/my', { replaceUrl: true });
  });

  it('redirects home when there is no stored return url', () => {
    const { router } = setup({ authed: true, returnUrl: null });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/', { replaceUrl: true });
  });

  it('shows the failure state when the session is not authenticated', () => {
    const { component, router } = setup({ authed: false });

    expect(component.failed()).toBe(true);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('retry() clears the failure and restarts the login flow', () => {
    const { component, auth } = setup({ authed: false, returnUrl: '/events' });

    component.retry();

    expect(component.failed()).toBe(false);
    expect(auth.login).toHaveBeenCalledWith('/events');
  });
});
