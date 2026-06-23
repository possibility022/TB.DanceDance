import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import {
  InviteLinkInfoModel,
  InviteLinkModel,
  ListInviteLinksResponse,
  RedeemInviteLinkResponse,
} from './api-models';

/** Single-use invite links granting membership/access to a group or event. */
@Injectable({ providedIn: 'root' })
export class InviteLinksService {
  private readonly api = inject(ApiClient);

  /** Admin: create an invite link for a group. */
  createForGroup(groupId: string): Observable<InviteLinkModel> {
    return this.api.post<InviteLinkModel>(
      `/api/groups/${encodeURIComponent(groupId)}/invite-links`,
    );
  }

  /** Admin: create an invite link for an event. */
  createForEvent(eventId: string): Observable<InviteLinkModel> {
    return this.api.post<InviteLinkModel>(
      `/api/events/${encodeURIComponent(eventId)}/invite-links`,
    );
  }

  /** Admin: list a group's invite links, regardless of who created each one. */
  listForGroup(groupId: string): Observable<ListInviteLinksResponse> {
    return this.api.get<ListInviteLinksResponse>(
      `/api/groups/${encodeURIComponent(groupId)}/invite-links`,
    );
  }

  /** Admin: list an event's invite links. */
  listForEvent(eventId: string): Observable<ListInviteLinksResponse> {
    return this.api.get<ListInviteLinksResponse>(
      `/api/events/${encodeURIComponent(eventId)}/invite-links`,
    );
  }

  /** Public: preview of an invite link, shown before sign-in. */
  getInfo(linkId: string): Observable<InviteLinkInfoModel> {
    return this.api.get<InviteLinkInfoModel>(`/api/invite-links/${encodeURIComponent(linkId)}`);
  }

  /** Redeem an invite link for the current user. */
  redeem(linkId: string): Observable<RedeemInviteLinkResponse> {
    return this.api.post<RedeemInviteLinkResponse>(
      `/api/invite-links/${encodeURIComponent(linkId)}/redeem`,
    );
  }

  /** Admin: revoke an invite link. */
  revoke(linkId: string): Observable<void> {
    return this.api.delete<void>(`/api/invite-links/${encodeURIComponent(linkId)}`);
  }
}
