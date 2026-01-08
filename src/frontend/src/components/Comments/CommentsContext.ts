import {CommentResponse} from "../../types/ApiModels/Comments/CommentResponse";
import React from "react";

export class CommentsContextService {
    comments: CommentResponse[] = [];
    commentsAvailable: boolean = false;
    commentsLoading: boolean = false;
    videoId?: string;
    linkId?: string;

    constructor() {
    }

    setVideoId(videoId: string) {
        this.videoId = videoId;
    }

    setLinkId(linkId: string) {
        this.linkId = linkId;
    }

    addCommentAsync(comment: string): Promise<void> {
        return Promise.resolve(undefined);
    }

    deleteCommentAsync(commentId: string): Promise<void> {
        return Promise.resolve(undefined);
    }

    loadCommentsAsync(): Promise<void> {
        this.comments = [
            {
                "id": "23474a48-a1e1-457c-b581-bd0e195a6725",
                "videoId": "c4029eb1-23ad-417b-9a3b-1b2ad4751c0b",
                "authorName": undefined,
                "content": "This is an anonymous comment - no auth token!",
                "createdAt": new Date(),
                "updatedAt": null,
                "isHidden": false,
                "isAnonymous": true,
                "isReported": false,
                "reportedReason": null,
                "isOwn": false,
                "canModerate": true
            },
            {
                "id": "96acc427-cf8e-4eb4-aaf8-09448a90b9ea",
                "videoId": "c4029eb1-23ad-417b-9a3b-1b2ad4751c0b",
                "authorName": "Tom B",
                "content": "This is a test comment from authenticated user!",
                "createdAt": new Date(),
                "updatedAt": null,
                "isAnonymous": false,
                "isHidden": false,
                "isReported": false,
                "reportedReason": null,
                "isOwn": true,
                "canModerate": true
            },
            {
                "id": "db8c7fff-a54d-437e-8ae8-29d3c155d595",
                "videoId": "c4029eb1-23ad-417b-9a3b-1b2ad4751c0b",
                "authorName": undefined,
                "content": "This is an anonymous comment - no auth token!",
                "createdAt": new Date(),
                "updatedAt": null,
                "isAnonymous": true,
                "isHidden": false,
                "isReported": false,
                "reportedReason": null,
                "isOwn": false,
                "canModerate": true
            }
        ]

        return Promise.resolve();
    }

    setComments(comments: CommentResponse[]): void {
    }

    setCommentsAvailable(b: boolean) {
        this.commentsAvailable = b;
    }
}

export const CommentsContext = React.createContext(new CommentsContextService())
