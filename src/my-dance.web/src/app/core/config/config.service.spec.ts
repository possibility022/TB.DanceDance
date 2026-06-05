import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ConfigService } from './config.service';
import { DEFAULT_CONFIG } from './app-config';

describe('ConfigService', () => {
  let service: ConfigService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ConfigService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ConfigService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('exposes the defaults before load() completes', () => {
    expect(service.config()).toEqual(DEFAULT_CONFIG);
  });

  it('fetches config.json and exposes the resolved values', () => {
    service.load().subscribe();

    const req = httpMock.expectOne('config.json');
    expect(req.request.method).toBe('GET');
    req.flush({
      apiBaseUrl: 'https://api.example.com',
      authUrl: 'https://auth.example.com',
      redirectUri: 'https://app.example.com/callback',
    });

    expect(service.config()).toEqual({
      apiBaseUrl: 'https://api.example.com',
      authUrl: 'https://auth.example.com',
      redirectUri: 'https://app.example.com/callback',
    });
  });

  it('trims surrounding whitespace on every value', () => {
    service.load().subscribe();
    httpMock.expectOne('config.json').flush({
      apiBaseUrl: '  https://api.example.com  ',
      authUrl: '\thttps://auth.example.com\n',
      redirectUri: ' https://app.example.com/callback ',
    });

    expect(service.config()).toEqual({
      apiBaseUrl: 'https://api.example.com',
      authUrl: 'https://auth.example.com',
      redirectUri: 'https://app.example.com/callback',
    });
  });

  it('falls back to defaults for blank apiBaseUrl / authUrl', () => {
    service.load().subscribe();
    httpMock.expectOne('config.json').flush({ apiBaseUrl: '   ', authUrl: '' });

    const config = service.config();
    expect(config.apiBaseUrl).toBe(DEFAULT_CONFIG.apiBaseUrl);
    expect(config.authUrl).toBe(DEFAULT_CONFIG.authUrl);
  });

  it('derives redirectUri from the window origin when blank', () => {
    service.load().subscribe();
    httpMock.expectOne('config.json').flush({ redirectUri: '   ' });

    expect(service.config().redirectUri).toBe(`${window.location.origin}/callback`);
  });

  it('falls back to the full default config on HTTP error', () => {
    let resolved = undefined as unknown;
    service.load().subscribe((c) => (resolved = c));

    httpMock.expectOne('config.json').flush('boom', { status: 500, statusText: 'Server Error' });

    expect(resolved).toEqual(DEFAULT_CONFIG);
    expect(service.config()).toEqual(DEFAULT_CONFIG);
  });

  it('fetches config.json only once across repeated load() calls', () => {
    service.load().subscribe();
    service.load().subscribe();

    // A single matching request proves the shared/cached observable.
    httpMock.expectOne('config.json').flush({ apiBaseUrl: 'https://api.example.com' });

    service.load().subscribe();
    httpMock.expectNone('config.json');
  });
});
