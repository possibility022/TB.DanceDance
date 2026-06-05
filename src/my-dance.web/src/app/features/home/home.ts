import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home.html',
})
export class Home {
  private readonly auth = inject(AuthService);

  readonly isAuthenticated = this.auth.isAuthenticated;

  login(): void {
    this.auth.login();
  }
}
