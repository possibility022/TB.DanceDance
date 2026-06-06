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

  it('getGroupVideos() GETs /api/groups/videos', () => {
    service.getGroupVideos().subscribe();
    const req = httpMock.expectOne(`${BASE}/api/groups/videos`);
    expect(req.request.method).toBe('GET');
    req.flush({ videos: [] });
  });

  it('getVideosForGroup() GETs the url-encoded group videos', () => {
    service.getVideosForGroup('g/1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/groups/g%2F1/videos`);
    expect(req.request.method).toBe('GET');
    req.flush({ videos: [] });
  });
});
