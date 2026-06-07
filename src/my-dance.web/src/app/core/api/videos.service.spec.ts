import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';

import { VideosService } from './videos.service';
import { ConfigService } from '../config/config.service';

const BASE = 'https://api.test';

describe('VideosService', () => {
  let service: VideosService;
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
    service = TestBed.inject(VideosService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getMyVideos() GETs /api/videos/my with page and pageSize params', () => {
    let response: unknown;
    service.getMyVideos(2, 20).subscribe((r) => (response = r));

    const req = httpMock.expectOne(`${BASE}/api/videos/my?page=2&pageSize=20`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [{ videoId: '1' }], totalCount: 1, pageNumber: 2, pageSize: 20 });

    expect(response).toEqual({ items: [{ videoId: '1' }], totalCount: 1, pageNumber: 2, pageSize: 20 });
  });

  it('getVideo() GETs the url-encoded blob id', () => {
    service.getVideo('a/b').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/videos/a%2Fb`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('renameVideo() POSTs the new name to the rename endpoint', () => {
    service.renameVideo('vid 1', { newName: 'Cha-cha' }).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/videos/vid%201/rename`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ newName: 'Cha-cha' });
    req.flush(null);
  });

  it('updateCommentSettings() POSTs the visibility to the comment-settings endpoint', () => {
    service.updateCommentSettings('vid1', { commentVisibility: 2 }).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/videos/vid1/comment-settings`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ commentVisibility: 2 });
    req.flush(null);
  });

  describe('streamUrl()', () => {
    it('builds an absolute stream url carrying the token as a query param', () => {
      expect(service.streamUrl('blob1', 'abc.def')).toBe(
        `${BASE}/api/videos/blob1/stream?token=abc.def`,
      );
    });

    it('url-encodes the blob id', () => {
      expect(service.streamUrl('a/b', 'tok')).toBe(`${BASE}/api/videos/a%2Fb/stream?token=tok`);
    });

    it('encodes a token with reserved characters', () => {
      const url = service.streamUrl('blob1', 'a b&c');
      expect(url.startsWith(`${BASE}/api/videos/blob1/stream?token=`)).toBe(true);
      expect(url).not.toContain('a b&c');
    });
  });
});
