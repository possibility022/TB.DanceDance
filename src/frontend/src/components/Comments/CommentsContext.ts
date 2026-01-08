import {CommentResponse} from "../../types/ApiModels/Comments/CommentResponse";
import React from "react";

export interface ICommentsContext {
    comments: CommentResponse[]
    commentsAvailable: boolean
    commentsLoading: boolean
    addCommentAsync: (comment: string) => Promise<void>
    deleteCommentAsync: (commentId: string) => Promise<void>
    loadCommentsAsync: () => Promise<void>
}

export const CommentsContext = React.createContext<ICommentsContext | undefined>(undefined)
