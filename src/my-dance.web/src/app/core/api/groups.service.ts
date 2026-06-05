import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import { ListGroupVideosResponse } from './api-models';

/** Recordings from the user's regular-lesson groups. */
@Injectable({ providedIn: 'root' })
export class GroupsService {
  private readonly api = inject(ApiClient);

  /** Recordings across all groups the user has access to. */
  getGroupVideos(): Observable<ListGroupVideosResponse> {
    return this.api.get<ListGroupVideosResponse>('/api/groups/videos');
  }

  /** Recordings for a single group. */
  getVideosForGroup(groupId: string): Observable<ListGroupVideosResponse> {
    return this.api.get<ListGroupVideosResponse>(
      `/api/groups/${encodeURIComponent(groupId)}/videos`,
    );
  }
}
