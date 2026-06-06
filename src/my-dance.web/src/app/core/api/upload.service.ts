import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import {
  ProduceUploadUrlRequest,
  ProduceUploadUrlResponse,
  RefreshUploadUrlResponse,
} from './api-models';

/**
 * Upload flow: the API issues a SAS URL the client uploads the file to directly,
 * then records a pending conversion job.
 */
@Injectable({ providedIn: 'root' })
export class UploadService {
  private readonly api = inject(ApiClient);

  /** Request a SAS upload URL for a new recording. */
  produceUploadUrl(request: ProduceUploadUrlRequest): Observable<ProduceUploadUrlResponse> {
    return this.api.post<ProduceUploadUrlResponse>('/api/videos/upload', request);
  }

  /** Get a fresh SAS URL for an in-progress upload (e.g. after expiry). */
  refreshUploadUrl(videoId: string): Observable<RefreshUploadUrlResponse> {
    return this.api.get<RefreshUploadUrlResponse>(
      `/api/videos/upload/${encodeURIComponent(videoId)}`,
    );
  }
}
