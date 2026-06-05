import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of, shareReplay, tap } from 'rxjs';

import { AppConfig, DEFAULT_CONFIG } from './app-config';

/**
 * Loads `config.json` once at startup and exposes the resolved configuration.
 * Missing or malformed values fall back to {@link DEFAULT_CONFIG} so the app
 * still runs locally without a config file.
 */
@Injectable({ providedIn: 'root' })
export class ConfigService {
  private readonly http = inject(HttpClient);

  private request$?: Observable<AppConfig>;
  private readonly _config = signal<AppConfig>(DEFAULT_CONFIG);

  /** The resolved configuration (defaults until {@link load} completes). */
  readonly config = this._config.asReadonly();

  /**
   * Fetches and caches the configuration. Safe to call multiple times — the
   * underlying HTTP request is shared, so config is fetched only once.
   */
  load(): Observable<AppConfig> {
    this.request$ ??= this.http.get<Partial<AppConfig>>('config.json').pipe(
      map((raw) => this.normalize(raw)),
      catchError(() => of(DEFAULT_CONFIG)),
      tap((config) => this._config.set(config)),
      shareReplay(1),
    );
    return this.request$;
  }

  private normalize(raw: Partial<AppConfig> | null): AppConfig {
    const redirectUri = raw?.redirectUri?.trim();
    return {
      apiBaseUrl: raw?.apiBaseUrl?.trim() || DEFAULT_CONFIG.apiBaseUrl,
      authUrl: raw?.authUrl?.trim() || DEFAULT_CONFIG.authUrl,
      redirectUri: redirectUri || `${window.location.origin}/callback`,
    };
  }
}
