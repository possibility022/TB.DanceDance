import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import { AnonymousIdService } from '../anonymous-id.service';
import {
  CommentResponse,
  CreateCommentRequest,
  PagedResponseOfCommentResponse,
  ReportCommentRequest,
  UpdateCommentRequest,
} from './api-models';

/**
 * Comments on recordings — both in the authenticated player (read) and on
 * public shared links (read + write, incl. anonymous), plus moderation actions.
 *
 * The anonymous-id is sent (header on read/delete, body field on add/edit) so
 * the backend can attribute and authorize a not-logged-in user's own comments
 * on a shared link.
 */
@Injectable({ providedIn: 'root' })
export class CommentsService {
  private readonly api = inject(ApiClient);
  private readonly anonymousId = inject(AnonymousIdService);

  private anonymousHeader(): Record<string, string> {
    return { AnonymousId: this.anonymousId.getId() };
  }

  /** A page of comments for a recording, in the authenticated player. */
  getCommentsForVideo(videoId: string, page: number, pageSize: number): Observable<PagedResponseOfCommentResponse> {
    return this.api.get<PagedResponseOfCommentResponse>(
      `/api/comments/video/${encodeURIComponent(videoId)}`,
      { params: { page, pageSize } },
    );
  }

  /** A page of comments on a shared link (public). */
  getCommentsByLink(linkId: string, page: number, pageSize: number): Observable<PagedResponseOfCommentResponse> {
    return this.api.get<PagedResponseOfCommentResponse>(
      `/api/share/${encodeURIComponent(linkId)}/comments`,
      { headers: this.anonymousHeader(), params: { page, pageSize } },
    );
  }

  /** Post a comment via a shared link (logged-in or anonymous). */
  addCommentByLink(linkId: string, request: CreateCommentRequest): Observable<CommentResponse> {
    return this.api.post<CommentResponse>(
      `/api/share/${encodeURIComponent(linkId)}/comments`,
      request,
    );
  }

  /** Author: edit own comment (carries the anonymous-id for guest authors). */
  updateComment(commentId: string, request: UpdateCommentRequest): Observable<void> {
    return this.api.put<void>(`/api/comments/${encodeURIComponent(commentId)}`, {
      ...request,
      anonymousId: this.anonymousId.getId(),
    });
  }

  /** Author or moderator: delete a comment. */
  deleteComment(commentId: string): Observable<void> {
    return this.api.delete<void>(`/api/comments/${encodeURIComponent(commentId)}`, {
      headers: this.anonymousHeader(),
    });
  }

  /** Moderator: hide a comment. */
  hideComment(commentId: string): Observable<void> {
    return this.api.put<void>(`/api/comments/${encodeURIComponent(commentId)}/hide`);
  }

  /** Moderator: unhide a comment. */
  unhideComment(commentId: string): Observable<void> {
    return this.api.put<void>(`/api/comments/${encodeURIComponent(commentId)}/unhide`);
  }

  /** Report a comment for moderation. */
  reportComment(commentId: string, request: ReportCommentRequest): Observable<void> {
    return this.api.post<void>(`/api/comments/${encodeURIComponent(commentId)}/report`, request);
  }
}
