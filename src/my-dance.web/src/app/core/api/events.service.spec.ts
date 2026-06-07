import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';

import { EventsService } from './events.service';
import { ConfigService } from '../config/config.service';

const BASE = 'https://api.test';

describe('EventsService', () => {
  let service: EventsService;
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
    service = TestBed.inject(EventsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('createEvent() POSTs the new event', () => {
    const body = { event: { name: 'Gala', date: new Date('2026-01-01') } };
    service.createEvent(body).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/events`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ id: 'e1' });
  });

  it('getEventVideos() GETs the url-encoded event videos with page and pageSize params', () => {
    let response;
    service.getEventVideos('e/1', 1, 20).subscribe((r) => (response = r));
    const req = httpMock.expectOne(`${BASE}/api/events/e%2F1/videos?page=1&pageSize=20`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [{ videoId: '1' }], totalCount: 1, pageNumber: 1, pageSize: 20 });

    expect(response).toEqual({ items: [{ videoId: '1' }], totalCount: 1, pageNumber: 1, pageSize: 20 });
  });
});
