import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="has-text-centered py-6">
      <h1 class="title">Page not found</h1>
      <p class="subtitle">The page you’re looking for doesn’t exist.</p>
      <a class="button is-primary" routerLink="/">Back to home</a>
    </div>
  `,
})
export class NotFound {}
