import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';

import { SharingService } from './sharing.service';
import { ConfigService } from '../config/config.service';

const BASE = 'https://api.test';

describe('SharingService', () => {
  let service: SharingService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ConfigService,
          useValue: { config: signal({ apiBaseUrl: BASE, authUrl: '', redirectUri: '' }) },
        },
      ],
    });
    service = TestBed.inject(SharingService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('createSharedLink() POSTs the request to the video share endpoint', () => {
    const body = { expirationDays: 7, allowComments: true, allowAnonymousComments: false };
    service.createSharedLink('vid1', body).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/videos/vid1/share`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ linkId: 'l1' });
  });

  it('getMySharedLinks() GETs /api/share/my', () => {
    service.getMySharedLinks().subscribe();
    const req = httpMock.expectOne(`${BASE}/api/share/my`);
    expect(req.request.method).toBe('GET');
    req.flush({ links: [] });
  });

  it('getSharedVideo() GETs the url-encoded link id', () => {
    service.getSharedVideo('a/b').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/share/a%2Fb`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('revokeSharedLink() DELETEs the link', () => {
    service.revokeSharedLink('l1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/share/l1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('sharedStreamUrl() builds an absolute, link-scoped stream url', () => {
    expect(service.sharedStreamUrl('l1')).toBe(`${BASE}/api/share/l1/stream`);
  });

  it('sharedStreamUrl() url-encodes the link id', () => {
    expect(service.sharedStreamUrl('a/b')).toBe(`${BASE}/api/share/a%2Fb/stream`);
  });
});
