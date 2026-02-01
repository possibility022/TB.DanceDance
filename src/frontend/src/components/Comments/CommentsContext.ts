import {CommentResponse} from "../../types/ApiModels/Comments/CommentResponse";
import React from "react";

export interface ICommentsContext {
    comments?: CommentResponse[]
    commentsLoading: boolean
    addCommentAsync: (comment: string) => Promise<void>
    addCommentAsAnonymouseAsync: (comment: string, authorName: string) => Promise<void>
    loadCommentsAsync(): Promise<void>
    hideCommentAsync(commentId: string, hide: boolean): Promise<void>
    editCommentAsync(commentId: string, newContent: string): Promise<void>
    reportCommentAsync(commentId: string, why: string): Promise<void>
    deleteCommentAsync(commentId: string): Promise<void>
}

export const CommentsContext = React.createContext<ICommentsContext | undefined>(undefined)
