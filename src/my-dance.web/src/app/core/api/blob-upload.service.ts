import { Injectable, InjectionToken, inject } from '@angular/core';
import { BlockBlobClient } from '@azure/storage-blob';
import { Observable } from 'rxjs';

/** The slice of {@link BlockBlobClient} this service depends on. */
export type UploadClient = Pick<BlockBlobClient, 'uploadData'>;

/**
 * Builds the Azure blob client for a SAS URL. Injectable so tests can supply a
 * fake without mocking the `@azure/storage-blob` module (module mocking proved
 * unreliable across environments).
 */
export const BLOCK_BLOB_CLIENT_FACTORY = new InjectionToken<(sasUrl: string) => UploadClient>(
  'BLOCK_BLOB_CLIENT_FACTORY',
  { providedIn: 'root', factory: () => (sasUrl: string) => new BlockBlobClient(sasUrl) },
);

/**
 * Uploads a file directly to Azure Blob Storage using a SAS URL issued by the
 * API. Targets the storage host (not our API), so the OIDC interceptor's
 * secureRoutes does not attach a bearer token.
 *
 * Delegates to the Azure SDK's {@link BlockBlobClient.uploadData}, which chunks
 * large files into blocks, uploads them in parallel, and commits the block list
 * automatically. Emits overall progress as a whole-number percentage and
 * completes once the blob is committed. Unsubscribing aborts the upload.
 */
@Injectable({ providedIn: 'root' })
export class BlobUploadService {
  private readonly createClient = inject(BLOCK_BLOB_CLIENT_FACTORY);

  upload(sasUrl: string, file: File): Observable<number> {
    return new Observable<number>((subscriber) => {
      const client = this.createClient(sasUrl);
      const controller = new AbortController();

      client
        .uploadData(file, {
          abortSignal: controller.signal,
          blobHTTPHeaders: { blobContentType: file.type || 'application/octet-stream' },
          onProgress: ({ loadedBytes }) => {
            subscriber.next(file.size > 0 ? Math.round((100 * loadedBytes) / file.size) : 100);
          },
        })
        .then(() => {
          subscriber.next(100);
          subscriber.complete();
        })
        .catch((error: unknown) => {
          if (!controller.signal.aborted) {
            subscriber.error(error);
          }
        });

      return () => controller.abort();
    });
  }
}
