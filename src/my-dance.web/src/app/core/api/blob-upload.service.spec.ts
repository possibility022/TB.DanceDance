import { TestBed } from '@angular/core/testing';
import { HttpEventType, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { BlobUploadService } from './blob-upload.service';

const BLOCK_SIZE = 8 * 1024 * 1024;
const SAS = 'https://blob.test/container/blob?sig=abc';

/** A File whose reported size we can inflate without allocating the bytes. */
function fakeFile(size: number, type = 'video/mp4'): File {
  const file = new File([new Uint8Array(1)], 'video.mp4', { type });
  Object.defineProperty(file, 'size', { value: size });
  return file;
}

describe('BlobUploadService', () => {
  let service: BlobUploadService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [BlobUploadService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(BlobUploadService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('single PUT (file at or below the block size)', () => {
    it('PUTs the whole file with block-blob headers', () => {
      const file = new File([new Uint8Array(10)], 'small.mp4', { type: 'video/mp4' });
      service.upload(SAS, file).subscribe();

      const req = httpMock.expectOne(SAS);
      expect(req.request.method).toBe('PUT');
      expect(req.request.headers.get('x-ms-blob-type')).toBe('BlockBlob');
      expect(req.request.headers.get('Content-Type')).toBe('video/mp4');
      expect(req.request.body).toBe(file);
      req.flush(null);
    });

    it('falls back to application/octet-stream when the file has no type', () => {
      const file = new File([new Uint8Array(10)], 'small', { type: '' });
      service.upload(SAS, file).subscribe();

      const req = httpMock.expectOne(SAS);
      expect(req.request.headers.get('Content-Type')).toBe('application/octet-stream');
      req.flush(null);
    });

    it('maps upload-progress events to whole-number percentages', () => {
      const file = new File([new Uint8Array(10)], 'small.mp4', { type: 'video/mp4' });
      const emissions: number[] = [];
      service.upload(SAS, file).subscribe((p) => emissions.push(p));

      const req = httpMock.expectOne(SAS);
      req.event({ type: HttpEventType.UploadProgress, loaded: 5, total: 10 });
      req.event({ type: HttpEventType.UploadProgress, loaded: 10, total: 10 });
      req.flush(null);

      expect(emissions).toEqual([50, 100]);
    });

    it('ignores progress events that carry no total', () => {
      const file = new File([new Uint8Array(10)], 'small.mp4', { type: 'video/mp4' });
      const emissions: number[] = [];
      service.upload(SAS, file).subscribe((p) => emissions.push(p));

      const req = httpMock.expectOne(SAS);
      req.event({ type: HttpEventType.UploadProgress, loaded: 5, total: 0 });
      req.flush(null);

      expect(emissions).toEqual([]);
    });

    it('uses a single PUT for a file of exactly the block size', () => {
      const file = fakeFile(BLOCK_SIZE);
      service.upload(SAS, file).subscribe();

      // No &comp=block query — the request targets the bare SAS url.
      const req = httpMock.expectOne(SAS);
      expect(req.request.method).toBe('PUT');
      req.flush(null);
    });
  });

  describe('block upload (file above the block size)', () => {
    it('uploads each block then commits a block list, completing at 100', () => {
      const file = fakeFile(BLOCK_SIZE + 100);
      const id0 = btoa('block-00000000');
      const id1 = btoa('block-00000001');

      const emissions: number[] = [];
      let completed = false;
      service.upload(SAS, file).subscribe({
        next: (p) => emissions.push(p),
        complete: () => (completed = true),
      });

      // concat() serializes the requests: only one is open at a time.
      const block0 = httpMock.expectOne((r) => r.url.includes('comp=block'));
      expect(block0.request.method).toBe('PUT');
      expect(block0.request.url).toContain(`blockid=${encodeURIComponent(id0)}`);
      block0.event({ type: HttpEventType.UploadProgress, loaded: 4_000_000, total: BLOCK_SIZE + 100 });
      block0.flush(null);

      const block1 = httpMock.expectOne((r) => r.url.includes('comp=block'));
      expect(block1.request.url).toContain(`blockid=${encodeURIComponent(id1)}`);
      block1.flush(null);

      const commit = httpMock.expectOne((r) => r.url.includes('comp=blocklist'));
      expect(commit.request.method).toBe('PUT');
      expect(commit.request.headers.get('Content-Type')).toBe('application/xml');
      expect(commit.request.body).toContain(`<Latest>${id0}</Latest>`);
      expect(commit.request.body).toContain(`<Latest>${id1}</Latest>`);
      commit.flush(null);

      expect(completed).toBe(true);
      expect(emissions.every((p) => p >= 0 && p <= 100)).toBe(true);
      expect(emissions.at(-1)).toBe(100);
    });

    it('uses stable, equal-length, base64 block ids', () => {
      const file = fakeFile(BLOCK_SIZE * 2 + 1);
      service.upload(SAS, file).subscribe();

      const ids = [btoa('block-00000000'), btoa('block-00000001'), btoa('block-00000002')];
      // All ids decode to the same length before encoding (required by Azure).
      expect(new Set(ids.map((id) => id.length)).size).toBe(1);

      for (const id of ids) {
        const req = httpMock.expectOne((r) => r.url.includes(`blockid=${encodeURIComponent(id)}`));
        req.flush(null);
      }
      httpMock.expectOne((r) => r.url.includes('comp=blocklist')).flush(null);
    });
  });
});
