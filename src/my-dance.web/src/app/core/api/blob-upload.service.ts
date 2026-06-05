import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEvent, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

/**
 * Uploads a file directly to Azure Blob Storage using a SAS URL issued by the
 * API. This targets the storage host (not our API), so the OIDC interceptor's
 * secureRoutes does not attach a bearer token.
 *
 * Single Put Blob upload (Azure caps this at 256 MB). Larger files would need a
 * block-based upload — a future enhancement if needed.
 */
@Injectable({ providedIn: 'root' })
export class BlobUploadService {
  private readonly http = inject(HttpClient);

  upload(sasUrl: string, file: File): Observable<HttpEvent<unknown>> {
    const headers = new HttpHeaders({
      'x-ms-blob-type': 'BlockBlob',
      'Content-Type': file.type || 'application/octet-stream',
    });
    return this.http.put(sasUrl, file, { headers, reportProgress: true, observe: 'events' });
  }
}
