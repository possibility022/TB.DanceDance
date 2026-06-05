import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from './api-client';
import {
  CommentResponse,
  CreateCommentRequest,
  ListCommentsByLinkResponse,
  ListCommentsForVideoResponse,
  ReportCommentRequest,
  UpdateCommentRequest,
} from './api-models';

/**
 * Comments on recordings — both in the authenticated player (read) and on
 * public shared links (read + write, incl. anonymous), plus moderation actions.
 */
@Injectable({ providedIn: 'root' })
export class CommentsService {
  private readonly api = inject(ApiClient);

  /** Comments for a recording, in the authenticated player. */
  getCommentsForVideo(videoId: string): Observable<ListCommentsForVideoResponse> {
    return this.api.get<ListCommentsForVideoResponse>(
      `/api/comments/video/${encodeURIComponent(videoId)}`,
    );
  }

  /** Comments on a shared link (public). */
  getCommentsByLink(linkId: string): Observable<ListCommentsByLinkResponse> {
    return this.api.get<ListCommentsByLinkResponse>(
      `/api/share/${encodeURIComponent(linkId)}/comments`,
    );
  }

  /** Post a comment via a shared link (logged-in or anonymous). */
  addCommentByLink(linkId: string, request: CreateCommentRequest): Observable<CommentResponse> {
    return this.api.post<CommentResponse>(
      `/api/share/${encodeURIComponent(linkId)}/comments`,
      request,
    );
  }

  /** Author: edit own comment. */
  updateComment(commentId: string, request: UpdateCommentRequest): Observable<void> {
    return this.api.put<void>(`/api/comments/${encodeURIComponent(commentId)}`, request);
  }

  /** Author or moderator: delete a comment. */
  deleteComment(commentId: string): Observable<void> {
    return this.api.delete<void>(`/api/comments/${encodeURIComponent(commentId)}`);
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
