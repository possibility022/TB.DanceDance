import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { AuthService } from '../../core/auth/auth.service';
import { Dashboard } from './dashboard/dashboard';
import { Landing } from './landing/landing';

@Component({
  selector: 'app-home',
  imports: [Dashboard, Landing],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home.html',
})
export class Home {
  private readonly auth = inject(AuthService);

  readonly isAuthenticated = this.auth.isAuthenticated;
}
