import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import {
  AddGroupAdminRequest,
  CreateGroupRequest,
  CreateGroupResponse,
  ListGroupAdminsResponse,
  ListGroupMembersResponse,
  ListMyGroupsResponse,
  PagedResponseOfVideoFromGroupInformation,
  UpdateGroupMemberRequest,
} from './api-models';

/** Recordings from the user's regular-lesson groups, plus group lifecycle management. */
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

  /** Create a group; the caller becomes its first admin. */
  createGroup(request: CreateGroupRequest): Observable<CreateGroupResponse> {
    return this.api.post<CreateGroupResponse>('/api/groups', request);
  }

  /** Groups the current user administers. */
  listMyGroups(): Observable<ListMyGroupsResponse> {
    return this.api.get<ListMyGroupsResponse>('/api/groups/my');
  }

  /** Admin: list a group's admins. */
  listAdmins(groupId: string): Observable<ListGroupAdminsResponse> {
    return this.api.get<ListGroupAdminsResponse>(`/api/groups/${encodeURIComponent(groupId)}/admins`);
  }

  /** Admin: grant admin rights to another user. */
  addAdmin(groupId: string, request: AddGroupAdminRequest): Observable<void> {
    return this.api.post<void>(`/api/groups/${encodeURIComponent(groupId)}/admins`, request);
  }

  /** Admin: revoke another user's admin rights (the last admin cannot be removed). */
  removeAdmin(groupId: string, userId: string): Observable<void> {
    return this.api.delete<void>(
      `/api/groups/${encodeURIComponent(groupId)}/admins/${encodeURIComponent(userId)}`,
    );
  }

  /** Admin: list a group's members. */
  listMembers(groupId: string): Observable<ListGroupMembersResponse> {
    return this.api.get<ListGroupMembersResponse>(
      `/api/groups/${encodeURIComponent(groupId)}/members`,
    );
  }

  /** Admin: change a member's join date. */
  updateMember(
    groupId: string,
    userId: string,
    request: UpdateGroupMemberRequest,
  ): Observable<void> {
    return this.api.put<void>(
      `/api/groups/${encodeURIComponent(groupId)}/members/${encodeURIComponent(userId)}`,
      request,
    );
  }

  /** Admin: revoke a member's access. */
  removeMember(groupId: string, userId: string): Observable<void> {
    return this.api.delete<void>(
      `/api/groups/${encodeURIComponent(groupId)}/members/${encodeURIComponent(userId)}`,
    );
  }
}
