import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';

import { AccessService } from './access.service';
import { ConfigService } from '../config/config.service';

const BASE = 'https://api.test';

describe('AccessService', () => {
  let service: AccessService;
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
    service = TestBed.inject(AccessService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAllEventsAndGroups() GETs /api/videos/accesses', () => {
    service.getAllEventsAndGroups().subscribe();
    const req = httpMock.expectOne(`${BASE}/api/videos/accesses`);
    expect(req.request.method).toBe('GET');
    req.flush({ events: [], groups: [] });
  });

  it('getMyAccess() GETs /api/videos/accesses/my', () => {
    service.getMyAccess().subscribe();
    const req = httpMock.expectOne(`${BASE}/api/videos/accesses/my`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('requestAccess() POSTs the request payload', () => {
    const body = { events: ['e1'], groups: [{ id: 'g1', joinedDate: new Date('2026-01-01') }] };
    service.requestAccess(body).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/videos/accesses/request`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush(null);
  });

  it('listAccessRequests() GETs the pending requests', () => {
    service.listAccessRequests().subscribe();
    const req = httpMock.expectOne(`${BASE}/api/videos/accesses/requests`);
    expect(req.request.method).toBe('GET');
    req.flush({ accessRequests: [] });
  });

  it('approveAccessRequest() POSTs the decision', () => {
    const body = { requestId: 'r1', isGroup: true, isApproved: true };
    service.approveAccessRequest(body).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/videos/accesses/requests`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush(null);
  });
});
