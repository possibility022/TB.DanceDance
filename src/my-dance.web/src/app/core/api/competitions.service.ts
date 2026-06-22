import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import {
  CompetitionResponse,
  CreateCompetitionRequest,
  CreateSharedLinkRequest,
  ListMyCompetitionsResponse,
  RenameCompetitionRequest,
  SharedLinkResponse,
} from './api-models';

/** Owner-owned groupings of private recordings, shared as one combined feedback thread. */
@Injectable({ providedIn: 'root' })
export class CompetitionsService {
  private readonly api = inject(ApiClient);

  /** The current user's competitions, newest first. */
  getMyCompetitions(): Observable<ListMyCompetitionsResponse> {
    return this.api.get<ListMyCompetitionsResponse>('/api/competitions');
  }

  /** A single competition with its grouped recordings. */
  getCompetition(competitionId: string): Observable<CompetitionResponse> {
    return this.api.get<CompetitionResponse>(
      `/api/competitions/${encodeURIComponent(competitionId)}`,
    );
  }

  /** Create a new competition. */
  createCompetition(request: CreateCompetitionRequest): Observable<CompetitionResponse> {
    return this.api.post<CompetitionResponse>('/api/competitions', request);
  }

  /** Rename a competition. */
  renameCompetition(competitionId: string, request: RenameCompetitionRequest): Observable<void> {
    return this.api.patch<void>(
      `/api/competitions/${encodeURIComponent(competitionId)}`,
      request,
    );
  }

  /** Delete a competition (its recordings become standalone again). */
  deleteCompetition(competitionId: string): Observable<void> {
    return this.api.delete<void>(`/api/competitions/${encodeURIComponent(competitionId)}`);
  }

  /** Add one of the owner's recordings to a competition. */
  addVideo(competitionId: string, videoId: string): Observable<void> {
    return this.api.put<void>(
      `/api/competitions/${encodeURIComponent(competitionId)}/videos/${encodeURIComponent(videoId)}`,
    );
  }

  /** Remove a recording from a competition (leaves it standalone). */
  removeVideo(competitionId: string, videoId: string): Observable<void> {
    return this.api.delete<void>(
      `/api/competitions/${encodeURIComponent(competitionId)}/videos/${encodeURIComponent(videoId)}`,
    );
  }

  /** Create a shared link that targets the whole competition (one combined thread). */
  createSharedLink(
    competitionId: string,
    request: CreateSharedLinkRequest,
  ): Observable<SharedLinkResponse> {
    return this.api.post<SharedLinkResponse>(
      `/api/competitions/${encodeURIComponent(competitionId)}/share`,
      request,
    );
  }
}
