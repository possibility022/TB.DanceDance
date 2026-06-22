import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import {
  CreateSharedLinkRequest,
  ListMySharedLinksResponse,
  SharedLinkResponse,
  SharedVideoInfoResponse,
} from './api-models';

/** Expiring shared links to individual recordings (incl. public viewing). */
@Injectable({ providedIn: 'root' })
export class SharingService {
  private readonly api = inject(ApiClient);

  /** Owner: create a shared link for a recording. */
  createSharedLink(
    videoId: string,
    request: CreateSharedLinkRequest,
  ): Observable<SharedLinkResponse> {
    return this.api.post<SharedLinkResponse>(
      `/api/videos/${encodeURIComponent(videoId)}/share`,
      request,
    );
  }

  /** Owner: the user's shared links. */
  getMySharedLinks(): Observable<ListMySharedLinksResponse> {
    return this.api.get<ListMySharedLinksResponse>('/api/share/my');
  }

  /** Public: info about a shared recording, by link id. */
  getSharedVideo(linkId: string): Observable<SharedVideoInfoResponse> {
    return this.api.get<SharedVideoInfoResponse>(`/api/share/${encodeURIComponent(linkId)}`);
  }

  /** Owner: revoke a shared link. */
  revokeSharedLink(linkId: string): Observable<void> {
    return this.api.delete<void>(`/api/share/${encodeURIComponent(linkId)}`);
  }

  /**
   * Public stream URL for a shared recording. The link itself authorizes
   * viewing, so no user token is needed.
   */
  sharedStreamUrl(linkId: string): string {
    return this.api.url(`/api/share/${encodeURIComponent(linkId)}/stream`);
  }

  /**
   * Public stream URL for one specific recording reachable through a link
   * (the link's single video, or one of its competition's videos).
   */
  sharedVideoStreamUrl(linkId: string, videoId: string): string {
    return this.api.url(
      `/api/share/${encodeURIComponent(linkId)}/videos/${encodeURIComponent(videoId)}/stream`,
    );
  }
}
