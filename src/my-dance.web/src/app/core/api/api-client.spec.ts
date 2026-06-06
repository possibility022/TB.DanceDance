import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { WritableSignal, signal } from '@angular/core';

import { ApiClient } from './api-client';
import { ConfigService } from '../config/config.service';
import { AppConfig } from '../config/app-config';

describe('ApiClient', () => {
  let api: ApiClient;
  let httpMock: HttpTestingController;
  let cfg: WritableSignal<AppConfig>;

  beforeEach(() => {
    cfg = signal<AppConfig>({
      apiBaseUrl: 'https://api.test',
      authUrl: 'https://auth.test',
      redirectUri: 'https://app.test/callback',
    });
    TestBed.configureTestingModule({
      providers: [
        ApiClient,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ConfigService, useValue: { config: cfg } },
      ],
    });
    api = TestBed.inject(ApiClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('url()', () => {
    it('joins the base url with a leading-slash path', () => {
      expect(api.url('/api/videos/my')).toBe('https://api.test/api/videos/my');
    });

    it('inserts a separator when the path has no leading slash', () => {
      expect(api.url('api/videos/my')).toBe('https://api.test/api/videos/my');
    });

    it('strips trailing slashes from the configured base url', () => {
      cfg.set({ ...cfg(), apiBaseUrl: 'https://api.test///' });
      expect(api.url('/api/x')).toBe('https://api.test/api/x');
    });

    it('reflects later config changes', () => {
      cfg.set({ ...cfg(), apiBaseUrl: 'https://other.test' });
      expect(api.url('/api/x')).toBe('https://other.test/api/x');
    });
  });

  describe('verbs', () => {
    it('GET hits the resolved url and returns the body', () => {
      let body: unknown;
      api.get<{ ok: boolean }>('/api/x').subscribe((b) => (body = b));

      const req = httpMock.expectOne('https://api.test/api/x');
      expect(req.request.method).toBe('GET');
      req.flush({ ok: true });

      expect(body).toEqual({ ok: true });
    });

    it('POST sends the provided body', () => {
      api.post('/api/x', { a: 1 }).subscribe();
      const req = httpMock.expectOne('https://api.test/api/x');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ a: 1 });
      req.flush(null);
    });

    it('POST sends null when no body is given', () => {
      api.post('/api/x').subscribe();
      const req = httpMock.expectOne('https://api.test/api/x');
      expect(req.request.body).toBeNull();
      req.flush(null);
    });

    it('PUT sends the provided body', () => {
      api.put('/api/x', { b: 2 }).subscribe();
      const req = httpMock.expectOne('https://api.test/api/x');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ b: 2 });
      req.flush(null);
    });

    it('PUT sends null when no body is given', () => {
      api.put('/api/x').subscribe();
      const req = httpMock.expectOne('https://api.test/api/x');
      expect(req.request.body).toBeNull();
      req.flush(null);
    });

    it('DELETE hits the resolved url', () => {
      api.delete('/api/x').subscribe();
      const req = httpMock.expectOne('https://api.test/api/x');
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });

    it('forwards request options (params and headers)', () => {
      api
        .get('/api/x', { params: { page: 2 }, headers: { 'X-Test': 'yes' } })
        .subscribe();

      const req = httpMock.expectOne((r) => r.url === 'https://api.test/api/x');
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.headers.get('X-Test')).toBe('yes');
      req.flush(null);
    });
  });
});
