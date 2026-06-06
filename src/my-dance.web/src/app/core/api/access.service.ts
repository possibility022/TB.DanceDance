import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import {
  ApproveAccessRequestRequest,
  GetUserAccessResponse,
  ListAccessRequestsResponse,
  ListAllEventsAndGroupsResponse,
  RequestAccessRequest,
} from './api-models';

/** Group/event access: what's available, what the user has, and admin approval. */
@Injectable({ providedIn: 'root' })
export class AccessService {
  private readonly api = inject(ApiClient);

  /** All groups and events that exist (to request access to). */
  getAllEventsAndGroups(): Observable<ListAllEventsAndGroupsResponse> {
    return this.api.get<ListAllEventsAndGroupsResponse>('/api/videos/accesses');
  }

  /** The user's assigned, available, and pending access. */
  getMyAccess(): Observable<GetUserAccessResponse> {
    return this.api.get<GetUserAccessResponse>('/api/videos/accesses/my');
  }

  /** Submit a request for access to groups and/or events. */
  requestAccess(request: RequestAccessRequest): Observable<void> {
    return this.api.post<void>('/api/videos/accesses/request', request);
  }

  /** Admin: list pending access requests. */
  listAccessRequests(): Observable<ListAccessRequestsResponse> {
    return this.api.get<ListAccessRequestsResponse>('/api/videos/accesses/requests');
  }

  /** Admin: approve or reject an access request. */
  approveAccessRequest(request: ApproveAccessRequestRequest): Observable<void> {
    return this.api.post<void>('/api/videos/accesses/requests', request);
  }
}
