import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';

import { CommentsService } from './comments.service';
import { ConfigService } from '../config/config.service';
import { AnonymousIdService } from '../anonymous-id.service';

const BASE = 'https://api.test';
const ANON_ID = 'anon-123';

describe('CommentsService', () => {
  let service: CommentsService;
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
        { provide: AnonymousIdService, useValue: { getId: () => ANON_ID } },
      ],
    });
    service = TestBed.inject(CommentsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getCommentsForVideo() GETs the authenticated video comments with page and pageSize params', () => {
    service.getCommentsForVideo('vid/1', 2, 20).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/comments/video/vid%2F1?page=2&pageSize=20`);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0, pageNumber: 2, pageSize: 20 });
  });

  it('getCommentsByLink() GETs link comments with the AnonymousId header and page/pageSize params', () => {
    service.getCommentsByLink('link1', 2, 20).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/share/link1/comments?page=2&pageSize=20`);
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.get('AnonymousId')).toBe(ANON_ID);
    req.flush({ items: [], totalCount: 0, pageNumber: 2, pageSize: 20 });
  });

  it('addCommentByLink() POSTs the comment body', () => {
    const body = { content: 'Nice!', authorName: 'Guest', anonymousId: ANON_ID };
    service.addCommentByLink('link1', body).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/share/link1/comments`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ id: 'c1' });
  });

  it('updateComment() PUTs the content merged with the anonymous id', () => {
    service.updateComment('c1', { content: 'edited' }).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/comments/c1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ content: 'edited', anonymousId: ANON_ID });
    req.flush(null);
  });

  it('deleteComment() DELETEs with the AnonymousId header', () => {
    service.deleteComment('c1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/comments/c1`);
    expect(req.request.method).toBe('DELETE');
    expect(req.request.headers.get('AnonymousId')).toBe(ANON_ID);
    req.flush(null);
  });

  it('hideComment() PUTs the hide endpoint', () => {
    service.hideComment('c1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/comments/c1/hide`);
    expect(req.request.method).toBe('PUT');
    req.flush(null);
  });

  it('unhideComment() PUTs the unhide endpoint', () => {
    service.unhideComment('c1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/comments/c1/unhide`);
    expect(req.request.method).toBe('PUT');
    req.flush(null);
  });

  it('reportComment() POSTs the reason to the report endpoint', () => {
    service.reportComment('c1', { reason: 'spam' }).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/comments/c1/report`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reason: 'spam' });
    req.flush(null);
  });
});
