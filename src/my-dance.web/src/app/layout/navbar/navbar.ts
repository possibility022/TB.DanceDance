import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { ConfigService } from '../../core/config/config.service';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Navbar {
  private readonly auth = inject(AuthService);
  private readonly config = inject(ConfigService);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly menuOpen = signal(false);

  readonly privacyUrl = computed(() => `${this.config.config().authUrl}/policy/dancedanceapp`);

  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  login(): void {
    this.closeMenu();
    this.auth.login();
  }

  logout(): void {
    this.closeMenu();
    this.auth.logout().subscribe();
  }
}
