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

  it('createGroup() POSTs to /api/groups', () => {
    const body = { name: 'Beginners', seasonStart: new Date('2024-09-01'), seasonEnd: new Date('2025-08-31') };
    service.createGroup(body).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/groups`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ id: 'g1' });
  });

  it('listMyGroups() GETs the groups the current user administers', () => {
    let response;
    service.listMyGroups().subscribe((r) => (response = r));
    const req = httpMock.expectOne(`${BASE}/api/groups/my`);
    expect(req.request.method).toBe('GET');
    req.flush({ groups: [{ id: 'g1', name: 'Beginners' }] });

    expect(response).toEqual({ groups: [{ id: 'g1', name: 'Beginners' }] });
  });

  it('listAdmins() GETs the group admins', () => {
    service.listAdmins('g1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/groups/g1/admins`);
    expect(req.request.method).toBe('GET');
    req.flush({ admins: [] });
  });

  it('addAdmin() POSTs the user id to the admins collection', () => {
    service.addAdmin('g1', { userId: 'u2' }).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/groups/g1/admins`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ userId: 'u2' });
    req.flush(null);
  });

  it('removeAdmin() DELETEs the admin', () => {
    service.removeAdmin('g1', 'u2').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/groups/g1/admins/u2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('listMembers() GETs the group members', () => {
    service.listMembers('g1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/groups/g1/members`);
    expect(req.request.method).toBe('GET');
    req.flush({ members: [] });
  });

  it('updateMember() PUTs the new join date', () => {
    const body = { whenJoined: new Date('2024-01-02') };
    service.updateMember('g1', 'u2', body).subscribe();
    const req = httpMock.expectOne(`${BASE}/api/groups/g1/members/u2`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush(null);
  });

  it('removeMember() DELETEs the membership', () => {
    service.removeMember('g1', 'u2').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/groups/g1/members/u2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
