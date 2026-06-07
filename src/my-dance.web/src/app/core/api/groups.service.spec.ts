import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';

import { GroupsService } from './groups.service';
import { ConfigService } from '../config/config.service';

const BASE = 'https://api.test';

describe('GroupsService', () => {
  let service: GroupsService;
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
    service = TestBed.inject(GroupsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getGroupVideos() GETs /api/groups/videos with page and pageSize params', () => {
    let response;
    service.getGroupVideos(1, 20).subscribe((r) => (response = r));
    const req = httpMock.expectOne(`${BASE}/api/groups/videos?page=1&pageSize=20`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [{ videoId: '1' }], totalCount: 1, pageNumber: 1, pageSize: 20 });

    expect(response).toEqual({ items: [{ videoId: '1' }], totalCount: 1, pageNumber: 1, pageSize: 20 });
  });

  it('getVideosForGroup() GETs the url-encoded group videos with page and pageSize params', () => {
    let response;
    service.getVideosForGroup('g/1', 1, 20).subscribe((r) => (response = r));
    const req = httpMock.expectOne(`${BASE}/api/groups/g%2F1/videos?page=1&pageSize=20`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [{ videoId: '1' }], totalCount: 1, pageNumber: 1, pageSize: 20 });

    expect(response).toEqual({ items: [{ videoId: '1' }], totalCount: 1, pageNumber: 1, pageSize: 20 });
  });
});
