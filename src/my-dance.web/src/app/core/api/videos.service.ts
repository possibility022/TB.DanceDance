import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import {
  PagedResponseOfVideoInformation,
  RenameVideoRequest,
  UpdateCommentSettingsRequest,
  VideoInformationResponse,
} from './api-models';

/** The user's recordings plus per-video info and owner actions. */
@Injectable({ providedIn: 'root' })
export class VideosService {
  private readonly api = inject(ApiClient);

  /** A page of the current user's private recordings. */
  getMyVideos(page: number, pageSize: number): Observable<PagedResponseOfVideoInformation> {
    return this.api.get<PagedResponseOfVideoInformation>('/api/videos/my', {
      params: { page, pageSize },
    });
  }

  /** Information about a single recording by its blob id. */
  getVideo(blobId: string): Observable<VideoInformationResponse> {
    return this.api.get<VideoInformationResponse>(`/api/videos/${encodeURIComponent(blobId)}`);
  }

  /** Owner-only: rename a recording. */
  renameVideo(videoId: string, request: RenameVideoRequest): Observable<void> {
    return this.api.post<void>(`/api/videos/${encodeURIComponent(videoId)}/rename`, request);
  }

  /** Owner-only: change who may see this recording's comments. */
  updateCommentSettings(
    videoId: string,
    request: UpdateCommentSettingsRequest,
  ): Observable<void> {
    return this.api.post<void>(
      `/api/videos/${encodeURIComponent(videoId)}/comment-settings`,
      request,
    );
  }

  /**
   * Stream URL for a recording. The `<video>` element cannot send an auth
   * header, so the access token is carried as a query parameter.
   */
  streamUrl(blobId: string, accessToken: string): string {
    const token = new HttpParams().set('token', accessToken).toString();
    return `${this.api.url(`/api/videos/${encodeURIComponent(blobId)}/stream`)}?${token}`;
  }
}
