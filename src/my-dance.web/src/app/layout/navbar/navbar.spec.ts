import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { WritableSignal, signal } from '@angular/core';
import { of } from 'rxjs';

import { Navbar } from './navbar';
import { AuthService } from '../../core/auth/auth.service';
import { ConfigService } from '../../core/config/config.service';

describe('Navbar', () => {
  let authed: WritableSignal<boolean>;
  let login: ReturnType<typeof vi.fn>;
  let logout: ReturnType<typeof vi.fn>;
  let fixture: ComponentFixture<Navbar>;
  let component: Navbar;

  beforeEach(async () => {
    authed = signal(false);
    login = vi.fn();
    logout = vi.fn(() => of(null));

    await TestBed.configureTestingModule({
      imports: [Navbar],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { isAuthenticated: authed, login, logout } },
        {
          provide: ConfigService,
          useValue: { config: signal({ apiBaseUrl: '', authUrl: 'https://auth.test', redirectUri: '' }) },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Navbar);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('reflects the auth state', () => {
    expect(component.isAuthenticated()).toBe(false);
    authed.set(true);
    expect(component.isAuthenticated()).toBe(true);
  });

  it('toggles and closes the burger menu', () => {
    expect(component.menuOpen()).toBe(false);
    component.toggleMenu();
    expect(component.menuOpen()).toBe(true);
    component.toggleMenu();
    expect(component.menuOpen()).toBe(false);

    component.toggleMenu();
    component.closeMenu();
    expect(component.menuOpen()).toBe(false);
  });

  it('builds the privacy-policy url from the auth url', () => {
    expect(component.privacyUrl()).toBe('https://auth.test/policy/dancedanceapp');
  });

  it('login() closes the menu and starts the login flow', () => {
    component.toggleMenu();
    component.login();
    expect(login).toHaveBeenCalledTimes(1);
    expect(component.menuOpen()).toBe(false);
  });

  it('logout() closes the menu and signs the user out', () => {
    component.toggleMenu();
    component.logout();
    expect(logout).toHaveBeenCalledTimes(1);
    expect(component.menuOpen()).toBe(false);
  });

  describe('template', () => {
    it('shows a Log in button and hides nav links when signed out', () => {
      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('.navbar-end button')?.textContent).toContain('Log in');
      expect(el.textContent).not.toContain('My recordings');
    });

    it('shows nav links and a Log out button when signed in', () => {
      authed.set(true);
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      expect(el.querySelector('.navbar-end button')?.textContent).toContain('Log out');
      expect(el.textContent).toContain('My recordings');
    });

    it('clicking the auth button signs the user out when signed in', () => {
      authed.set(true);
      fixture.detectChanges();
      (fixture.nativeElement.querySelector('.navbar-end button') as HTMLButtonElement).click();
      expect(logout).toHaveBeenCalledTimes(1);
    });

    it('shows a "Manage groups" link under Access when signed in', () => {
      authed.set(true);
      fixture.detectChanges();

      const el = fixture.nativeElement as HTMLElement;
      const link = el.querySelector('a[href="/groups/manage"]');
      expect(link?.textContent).toContain('Manage groups');
    });
  });
});
