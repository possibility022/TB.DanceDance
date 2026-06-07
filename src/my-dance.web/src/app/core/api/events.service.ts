import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import {
  CreateNewEventRequest,
  CreateNewEventResponse,
  PagedResponseOfVideoInformation,
} from './api-models';

/** Events and their recordings. */
@Injectable({ providedIn: 'root' })
export class EventsService {
  private readonly api = inject(ApiClient);

  /** Create a new event. */
  createEvent(request: CreateNewEventRequest): Observable<CreateNewEventResponse> {
    return this.api.post<CreateNewEventResponse>('/api/events', request);
  }

  /** A page of recordings for a single event. */
  getEventVideos(
    eventId: string,
    page: number,
    pageSize: number,
  ): Observable<PagedResponseOfVideoInformation> {
    return this.api.get<PagedResponseOfVideoInformation>(
      `/api/events/${encodeURIComponent(eventId)}/videos`,
      { params: { page, pageSize } },
    );
  }
}
