import {CommentResponse} from "../../types/ApiModels/Comments/CommentResponse";
import React from "react";

export interface ICommentsContext {
    comments?: CommentResponse[]
    commentsLoading: boolean
    addCommentAsync: (comment: string, authorName?: string) => Promise<void>
    deleteCommentAsync: (commentId: string) => Promise<void>
    loadCommentsAsync: () => Promise<void>
}

export const CommentsContext = React.createContext<ICommentsContext | undefined>(undefined)
