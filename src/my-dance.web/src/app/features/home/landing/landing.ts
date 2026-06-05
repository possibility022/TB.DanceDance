import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-landing',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="hero">
      <div class="hero-body">
        <h1 class="title">Dance Dance</h1>
        <p class="subtitle">
          A home for your dance recordings — group lessons, events, and a private archive you can
          share with expiring links.
        </p>
        <button type="button" class="button is-primary is-medium" (click)="login()">Log in</button>
      </div>
    </section>
  `,
})
export class Landing {
  private readonly auth = inject(AuthService);

  login(): void {
    this.auth.login();
  }
}
