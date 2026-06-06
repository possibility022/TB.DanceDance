import { TestBed } from '@angular/core/testing';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { BehaviorSubject, of } from 'rxjs';

import { AuthService } from './auth.service';

const RETURN_URL_KEY = 'auth.returnUrl';

function createOidcMock() {
  return {
    isAuthenticated$: new BehaviorSubject<{ isAuthenticated: boolean; allConfigsAuthenticated: unknown[] }>(
      { isAuthenticated: false, allConfigsAuthenticated: [] },
    ),
    userData$: new BehaviorSubject<{ userData: unknown; allUserData: unknown[] }>({
      userData: null,
      allUserData: [],
    }),
    checkAuth: vi.fn(() => of({ isAuthenticated: true })),
    authorize: vi.fn(),
    logoff: vi.fn(() => of(null)),
    getAccessToken: vi.fn(() => of('access-token')),
  };
}

describe('AuthService', () => {
  let service: AuthService;
  let oidc: ReturnType<typeof createOidcMock>;

  beforeEach(() => {
    sessionStorage.clear();
    oidc = createOidcMock();
    TestBed.configureTestingModule({
      providers: [AuthService, { provide: OidcSecurityService, useValue: oidc }],
    });
    service = TestBed.inject(AuthService);
  });

  describe('reactive state', () => {
    it('starts unauthenticated with no user data', () => {
      expect(service.isAuthenticated()).toBe(false);
      expect(service.userData()).toBeNull();
    });

    it('reflects the OIDC authentication stream', () => {
      oidc.isAuthenticated$.next({ isAuthenticated: true, allConfigsAuthenticated: [] });
      expect(service.isAuthenticated()).toBe(true);
    });

    it('reflects the OIDC user-data stream', () => {
      oidc.userData$.next({ userData: { name: 'Ada' }, allUserData: [] });
      expect(service.userData()).toEqual({ name: 'Ada' });
    });
  });

  describe('actions', () => {
    it('checkAuth() delegates to the OIDC service', () => {
      service.checkAuth().subscribe();
      expect(oidc.checkAuth).toHaveBeenCalledTimes(1);
    });

    it('login() stores the return url and starts the flow', () => {
      service.login('/videos/my');
      expect(sessionStorage.getItem(RETURN_URL_KEY)).toBe('/videos/my');
      expect(oidc.authorize).toHaveBeenCalledTimes(1);
    });

    it('login() without a return url does not write session storage', () => {
      service.login();
      expect(sessionStorage.getItem(RETURN_URL_KEY)).toBeNull();
      expect(oidc.authorize).toHaveBeenCalledTimes(1);
    });

    it('logout() delegates to logoff()', () => {
      service.logout().subscribe();
      expect(oidc.logoff).toHaveBeenCalledTimes(1);
    });

    it('getAccessToken() delegates to the OIDC service', () => {
      let token: string | undefined;
      service.getAccessToken().subscribe((t) => (token = t));
      expect(token).toBe('access-token');
    });
  });

  describe('consumeReturnUrl', () => {
    it('reads and clears the stored return url', () => {
      sessionStorage.setItem(RETURN_URL_KEY, '/events');

      expect(service.consumeReturnUrl()).toBe('/events');
      expect(sessionStorage.getItem(RETURN_URL_KEY)).toBeNull();
    });

    it('returns null when nothing is stored', () => {
      expect(service.consumeReturnUrl()).toBeNull();
    });
  });
});
