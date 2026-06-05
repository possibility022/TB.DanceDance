import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { Navbar } from './layout/navbar/navbar';
import { CookieConsent } from './layout/cookie-consent/cookie-consent';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, CookieConsent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
