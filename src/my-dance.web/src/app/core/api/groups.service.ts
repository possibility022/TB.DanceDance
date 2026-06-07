import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import { PagedResponseOfVideoFromGroupInformation } from './api-models';

/** Recordings from the user's regular-lesson groups. */
@Injectable({ providedIn: 'root' })
export class GroupsService {
  private readonly api = inject(ApiClient);

  /** A page of recordings across all groups the user has access to. */
  getGroupVideos(
    page: number,
    pageSize: number,
  ): Observable<PagedResponseOfVideoFromGroupInformation> {
    return this.api.get<PagedResponseOfVideoFromGroupInformation>('/api/groups/videos', {
      params: { page, pageSize },
    });
  }

  /** A page of recordings for a single group. */
  getVideosForGroup(
    groupId: string,
    page: number,
    pageSize: number,
  ): Observable<PagedResponseOfVideoFromGroupInformation> {
    return this.api.get<PagedResponseOfVideoFromGroupInformation>(
      `/api/groups/${encodeURIComponent(groupId)}/videos`,
      { params: { page, pageSize } },
    );
  }
}
