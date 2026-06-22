import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ConfigService } from '../config/config.service';

export interface RequestOptions {
  params?: HttpParams | Record<string, string | number | boolean | readonly string[]>;
  headers?: HttpHeaders | Record<string, string | string[]>;
}

/**
 * Thin HTTP wrapper that resolves API-relative paths against the configured
 * `apiBaseUrl`. The bearer token is attached by the OIDC `authInterceptor`
 * (secureRoutes), so services never deal with auth headers directly.
 */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ConfigService);

  /** Absolute URL for an API-relative path (e.g. `/api/videos/my`). */
  url(path: string): string {
    const base = this.config.config().apiBaseUrl.replace(/\/+$/, '');
    return `${base}${path.startsWith('/') ? '' : '/'}${path}`;
  }

  get<T>(path: string, options?: RequestOptions): Observable<T> {
    return this.http.get<T>(this.url(path), options);
  }

  post<T>(path: string, body?: unknown, options?: RequestOptions): Observable<T> {
    return this.http.post<T>(this.url(path), body ?? null, options);
  }

  put<T>(path: string, body?: unknown, options?: RequestOptions): Observable<T> {
    return this.http.put<T>(this.url(path), body ?? null, options);
  }

  patch<T>(path: string, body?: unknown, options?: RequestOptions): Observable<T> {
    return this.http.patch<T>(this.url(path), body ?? null, options);
  }

  delete<T>(path: string, options?: RequestOptions): Observable<T> {
    return this.http.delete<T>(this.url(path), options);
  }
}
