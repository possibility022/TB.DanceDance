import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEventType, HttpHeaders } from '@angular/common/http';
import { Observable, concat, filter, map, tap } from 'rxjs';

/** 8 MB blocks; files at or below this go up in a single Put Blob. */
const BLOCK_SIZE = 8 * 1024 * 1024;

interface BlockRef {
  readonly id: string;
  readonly start: number;
  readonly end: number;
}

/**
 * Uploads a file directly to Azure Blob Storage using a SAS URL issued by the
 * API. Targets the storage host (not our API), so the OIDC interceptor's
 * secureRoutes does not attach a bearer token.
 *
 * Large files are uploaded as blocks (Put Block + Put Block List), which lifts
 * the 256 MB single-PUT ceiling. Emits overall progress as a percentage and
 * completes when the blob is committed.
 */
@Injectable({ providedIn: 'root' })
export class BlobUploadService {
  private readonly http = inject(HttpClient);

  upload(sasUrl: string, file: File): Observable<number> {
    return file.size > BLOCK_SIZE ? this.uploadInBlocks(sasUrl, file) : this.uploadSingle(sasUrl, file);
  }

  private uploadSingle(sasUrl: string, file: File): Observable<number> {
    const headers = new HttpHeaders({
      'x-ms-blob-type': 'BlockBlob',
      'Content-Type': file.type || 'application/octet-stream',
    });
    return this.http.put(sasUrl, file, { headers, reportProgress: true, observe: 'events' }).pipe(
      map((event) =>
        event.type === HttpEventType.UploadProgress && event.total
          ? Math.round((100 * event.loaded) / event.total)
          : null,
      ),
      filter((percent): percent is number => percent !== null),
    );
  }

  private uploadInBlocks(sasUrl: string, file: File): Observable<number> {
    const blocks = this.planBlocks(file.size);
    const total = file.size;
    let committed = 0;

    const uploads = blocks.map((block) => {
      const chunk = file.slice(block.start, block.end);
      const url = `${sasUrl}&comp=block&blockid=${encodeURIComponent(block.id)}`;
      return this.http.put(url, chunk, { reportProgress: true, observe: 'events' }).pipe(
        map((event) =>
          event.type === HttpEventType.UploadProgress
            ? Math.round((100 * (committed + event.loaded)) / total)
            : null,
        ),
        filter((percent): percent is number => percent !== null),
        tap({ complete: () => (committed += block.end - block.start) }),
      );
    });

    return concat(
      ...uploads,
      this.commitBlockList(sasUrl, file, blocks).pipe(map(() => 100)),
    );
  }

  private commitBlockList(sasUrl: string, file: File, blocks: readonly BlockRef[]): Observable<unknown> {
    const body = `<?xml version="1.0" encoding="utf-8"?><BlockList>${blocks
      .map((block) => `<Latest>${block.id}</Latest>`)
      .join('')}</BlockList>`;
    const headers = new HttpHeaders({
      'Content-Type': 'application/xml',
      'x-ms-blob-content-type': file.type || 'application/octet-stream',
    });
    return this.http.put(`${sasUrl}&comp=blocklist`, body, { headers });
  }

  private planBlocks(size: number): BlockRef[] {
    const blocks: BlockRef[] = [];
    for (let index = 0, start = 0; start < size; index++) {
      const end = Math.min(start + BLOCK_SIZE, size);
      // Block ids must be equal-length before base64 encoding.
      blocks.push({ id: btoa(`block-${String(index).padStart(8, '0')}`), start, end });
      start = end;
    }
    return blocks;
  }
}
