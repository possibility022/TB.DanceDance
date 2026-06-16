import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';

import { TransfersService } from './transfers.service';
import { ConfigService } from '../config/config.service';

const BASE = 'https://api.test';

describe('TransfersService', () => {
  let service: TransfersService;
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
    service = TestBed.inject(TransfersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('createTransfer() POSTs the request to /api/transfers', () => {
    const body = { videoIds: ['v1', 'v2'], expirationDays: 7 };
    service.createTransfer(body).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/transfers`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ linkId: 't1' });
  });

  it('getMyTransfers() GETs /api/transfers/my', () => {
    service.getMyTransfers().subscribe();
    const req = httpMock.expectOne(`${BASE}/api/transfers/my`);
    expect(req.request.method).toBe('GET');
    req.flush({ transfers: [] });
  });

  it('getTransfer() GETs the url-encoded link id', () => {
    service.getTransfer('a/b').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/transfers/a%2Fb`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('acceptTransfer() POSTs to the accept endpoint', () => {
    service.acceptTransfer('t1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/transfers/t1/accept`);
    expect(req.request.method).toBe('POST');
    req.flush({ accepted: true });
  });

  it('declineTransfer() POSTs to the decline endpoint', () => {
    service.declineTransfer('t1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/transfers/t1/decline`);
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });

  it('revokeTransfer() DELETEs the transfer', () => {
    service.revokeTransfer('t1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/transfers/t1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('transferStreamUrl() builds an absolute, token-carrying stream url', () => {
    expect(service.transferStreamUrl('t1', 'v1', 'tok')).toBe(
      `${BASE}/api/transfers/t1/videos/v1/stream?token=tok`,
    );
  });
});
