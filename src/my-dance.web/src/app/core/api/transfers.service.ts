import { Injectable, inject } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import {
  AcceptTransferResponse,
  CreateTransferRequest,
  ListMyTransfersResponse,
  TransferInfoResponse,
  TransferSummaryResponse,
} from './api-models';

/** Ownership transfers of private recordings to another user via a link. */
@Injectable({ providedIn: 'root' })
export class TransfersService {
  private readonly api = inject(ApiClient);

  /** Sender: create a transfer for one or more owned, private recordings. */
  createTransfer(request: CreateTransferRequest): Observable<TransferSummaryResponse> {
    return this.api.post<TransferSummaryResponse>('/api/transfers', request);
  }

  /** Sender: the user's outgoing transfers. */
  getMyTransfers(): Observable<ListMyTransfersResponse> {
    return this.api.get<ListMyTransfersResponse>('/api/transfers/my');
  }

  /** Recipient: info about a transfer by link id (auth required). */
  getTransfer(linkId: string): Observable<TransferInfoResponse> {
    return this.api.get<TransferInfoResponse>(`/api/transfers/${encodeURIComponent(linkId)}`);
  }

  /** Recipient: accept a transfer (moves ownership). */
  acceptTransfer(linkId: string): Observable<AcceptTransferResponse> {
    return this.api.post<AcceptTransferResponse>(
      `/api/transfers/${encodeURIComponent(linkId)}/accept`,
    );
  }

  /** Recipient: decline a transfer. */
  declineTransfer(linkId: string): Observable<void> {
    return this.api.post<void>(`/api/transfers/${encodeURIComponent(linkId)}/decline`);
  }

  /** Sender: revoke a pending transfer. */
  revokeTransfer(linkId: string): Observable<void> {
    return this.api.delete<void>(`/api/transfers/${encodeURIComponent(linkId)}`);
  }

  /**
   * Transfer-scoped stream URL for previewing an item. The `<video>` element
   * cannot send an auth header, so the access token is carried as a query param.
   */
  transferStreamUrl(linkId: string, videoId: string, accessToken: string): string {
    const token = new HttpParams().set('token', accessToken).toString();
    return `${this.api.url(
      `/api/transfers/${encodeURIComponent(linkId)}/videos/${encodeURIComponent(videoId)}/stream`,
    )}?${token}`;
  }
}
