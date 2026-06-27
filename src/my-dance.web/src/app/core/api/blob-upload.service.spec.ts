import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { BLOCK_BLOB_CLIENT_FACTORY, BlobUploadService, UploadClient } from './blob-upload.service';

const SAS = 'https://blob.test/container/blob?sig=abc';

/** Options uploadData receives; captured so tests can drive onProgress. */
interface UploadOptions {
  abortSignal?: AbortSignal;
  blobHTTPHeaders?: { blobContentType?: string };
  onProgress?: (event: { loadedBytes: number }) => void;
}

const uploadData =
  vi.fn<(data: unknown, options: UploadOptions) => Promise<unknown>>();

/** A File whose reported size we can set without allocating the bytes. */
function fakeFile(size: number, type = 'video/mp4'): File {
  const file = new File([new Uint8Array(1)], 'video.mp4', { type });
  Object.defineProperty(file, 'size', { value: size });
  return file;
}

/** Resolves the deferred-style mock so the test can flush completion on demand. */
function deferUpload(): { resolve: () => void; reject: (e: unknown) => void } {
  let resolve!: () => void;
  let reject!: (e: unknown) => void;
  uploadData.mockReturnValueOnce(
    new Promise((res, rej) => {
      resolve = () => res(undefined);
      reject = rej;
    }),
  );
  return { resolve, reject };
}

describe('BlobUploadService', () => {
  let service: BlobUploadService;

  beforeEach(() => {
    uploadData.mockReset();
    TestBed.configureTestingModule({
      providers: [
        BlobUploadService,
        {
          provide: BLOCK_BLOB_CLIENT_FACTORY,
          useValue: () => ({ uploadData }) as unknown as UploadClient,
        },
      ],
    });
    service = TestBed.inject(BlobUploadService);
  });

  it('uploads the file via the Azure SDK with the file content type', () => {
    const { resolve } = deferUpload();
    const file = fakeFile(1024);
    service.upload(SAS, file).subscribe();

    expect(uploadData).toHaveBeenCalledTimes(1);
    const [data, options] = uploadData.mock.calls[0];
    expect(data).toBe(file);
    expect(options.blobHTTPHeaders?.blobContentType).toBe('video/mp4');
    resolve();
  });

  it('falls back to application/octet-stream when the file has no type', () => {
    deferUpload();
    service.upload(SAS, fakeFile(1024, '')).subscribe();

    expect(uploadData.mock.calls[0][1].blobHTTPHeaders?.blobContentType).toBe(
      'application/octet-stream',
    );
  });

  it('maps reported bytes to whole-number percentages', () => {
    const { resolve } = deferUpload();
    const emissions: number[] = [];
    service.upload(SAS, fakeFile(1000)).subscribe((p) => emissions.push(p));

    const onProgress = uploadData.mock.calls[0][1].onProgress!;
    onProgress({ loadedBytes: 250 });
    onProgress({ loadedBytes: 753 });
    resolve();

    expect(emissions).toEqual([25, 75]);
  });

  it('emits 100 and completes once the upload resolves', async () => {
    const { resolve } = deferUpload();
    const emissions: number[] = [];
    let completed = false;
    service.upload(SAS, fakeFile(1000)).subscribe({
      next: (p) => emissions.push(p),
      complete: () => (completed = true),
    });

    resolve();
    await Promise.resolve();

    expect(emissions.at(-1)).toBe(100);
    expect(completed).toBe(true);
  });

  it('reports a rejected upload as an error', async () => {
    const { reject } = deferUpload();
    let errored: unknown;
    service.upload(SAS, fakeFile(1000)).subscribe({ error: (e) => (errored = e) });

    const failure = new Error('network');
    reject(failure);
    await Promise.resolve();
    await Promise.resolve();

    expect(errored).toBe(failure);
  });

  it('aborts the upload when the subscription is torn down', () => {
    deferUpload();
    const subscription = service.upload(SAS, fakeFile(1000)).subscribe();

    const signal = uploadData.mock.calls[0][1].abortSignal!;
    expect(signal.aborted).toBe(false);
    subscription.unsubscribe();
    expect(signal.aborted).toBe(true);
  });

  it('does not surface an abort-driven rejection as an error', async () => {
    const { reject } = deferUpload();
    let errored = false;
    const subscription = service
      .upload(SAS, fakeFile(1000))
      .subscribe({ error: () => (errored = true) });

    subscription.unsubscribe();
    reject(new Error('aborted'));
    await Promise.resolve();
    await Promise.resolve();

    expect(errored).toBe(false);
  });
});
