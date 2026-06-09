import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';
import { provideRuntimeConfig } from './core/config/config.providers';
import { provideAuthentication } from './core/auth/auth.config';
import { AuthService } from './core/auth/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes, withComponentInputBinding()),
    // XHR backend (the default) is required for upload-progress events;
    // withFetch()'s backend cannot report bytes-sent progress.
    provideHttpClient(withInterceptors([authInterceptor()])),
    provideRuntimeConfig(),
    provideAuthentication(),
    // Hydrate the session / process a sign-in redirect before routing starts.
    provideAppInitializer(() => firstValueFrom(inject(AuthService).checkAuth())),
  ],
};
