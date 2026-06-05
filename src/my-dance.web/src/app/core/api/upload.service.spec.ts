import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';

import { UploadService } from './upload.service';
import { ConfigService } from '../config/config.service';
import { ProduceUploadUrlRequest, SharingWithType } from './api-models';

const BASE = 'https://api.test';

describe('UploadService', () => {
  let service: UploadService;
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
    service = TestBed.inject(UploadService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('produceUploadUrl() POSTs the upload request', () => {
    const body: ProduceUploadUrlRequest = {
      nameOfVideo: 'Lesson',
      fileName: 'lesson.mp4',
      sharedWith: 'g1',
      sharingWithType: SharingWithType.Group,
    };
    let response: unknown;
    service.produceUploadUrl(body).subscribe((r) => (response = r));

    const req = httpMock.expectOne(`${BASE}/api/videos/upload`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ sas: 'https://blob/sas', videoId: 'v1' });

    expect(response).toEqual({ sas: 'https://blob/sas', videoId: 'v1' });
  });

  it('refreshUploadUrl() GETs a fresh SAS for the url-encoded video id', () => {
    service.refreshUploadUrl('v/1').subscribe();
    const req = httpMock.expectOne(`${BASE}/api/videos/upload/v%2F1`);
    expect(req.request.method).toBe('GET');
    req.flush({ sas: 'https://blob/sas2' });
  });
});
