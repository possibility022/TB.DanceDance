import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';

import { CompetitionsService } from './competitions.service';
import { ConfigService } from '../config/config.service';

const BASE = 'https://api.test';

describe('CompetitionsService', () => {
  let service: CompetitionsService;
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
    service = TestBed.inject(CompetitionsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getMyCompetitions() GETs /api/competitions', () => {
    service.getMyCompetitions().subscribe();
    const req = httpMock.expectOne(`${BASE}/api/competitions`);
    expect(req.request.method).toBe('GET');
    req.flush({ competitions: [] });
  });

  it('getCompetition() GETs the url-encoded id', () => {
    service.getCompetition('c1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/competitions/c1`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('createCompetition() POSTs the request', () => {
    const body = { name: 'Nationals', commentVisibility: 1 };
    service.createCompetition(body).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/competitions`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ id: 'c1' });
  });

  it('renameCompetition() PATCHes the competition', () => {
    service.renameCompetition('c1', { newName: 'New' }).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/competitions/c1`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ newName: 'New' });
    req.flush(null);
  });

  it('deleteCompetition() DELETEs the competition', () => {
    service.deleteCompetition('c1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/competitions/c1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('addVideo() PUTs the competition/video pair', () => {
    service.addVideo('c1', 'v1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/competitions/c1/videos/v1`);
    expect(req.request.method).toBe('PUT');
    req.flush(null);
  });

  it('removeVideo() DELETEs the competition/video pair', () => {
    service.removeVideo('c1', 'v1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/competitions/c1/videos/v1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('createSharedLink() POSTs to the competition share route', () => {
    const body = { expirationDays: 7, allowComments: true, allowAnonymousComments: false };
    service.createSharedLink('c1', body).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/competitions/c1/share`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ linkId: 'l1' });
  });
});
