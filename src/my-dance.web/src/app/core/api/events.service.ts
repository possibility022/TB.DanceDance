import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import {
  CreateNewEventRequest,
  CreateNewEventResponse,
  ListEventVideosResponse,
} from './api-models';

/** Events and their recordings. */
@Injectable({ providedIn: 'root' })
export class EventsService {
  private readonly api = inject(ApiClient);

  /** Create a new event. */
  createEvent(request: CreateNewEventRequest): Observable<CreateNewEventResponse> {
    return this.api.post<CreateNewEventResponse>('/api/events', request);
  }

  /** Recordings for a single event. */
  getEventVideos(eventId: string): Observable<ListEventVideosResponse> {
    return this.api.get<ListEventVideosResponse>(
      `/api/events/${encodeURIComponent(eventId)}/videos`,
    );
  }
}
