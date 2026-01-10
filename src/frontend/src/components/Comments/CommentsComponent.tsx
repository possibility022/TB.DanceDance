import React, {useEffect, useState} from 'react';
import commentsService from "../../services/CommentsService";
import {CommentResponse} from "../../types/ApiModels/Comments/CommentResponse";
import {CommentsContext} from "./CommentsContext";
import CommentsList from "./CommentsList";
import AddComment from "./AddComment";

interface ICommentsComponentProps {
    videoId: string
    allowAdding: boolean
    linkId?: string
}

function CommentsComponent(props: ICommentsComponentProps) {

    const [comments, setComments] = useState<CommentResponse[]>([])
    const [commentsLoading, setCommentsLoading] = useState(false)

    useEffect(() => {
        loadCommentsAsync()
            .catch(console.error)
    }, []);

    const loadCommentsAsync = async () => {
        try {
            setCommentsLoading(true)

            let comments: CommentResponse[] = []

            if (props.linkId) {
                comments = await commentsService.getCommentsByLink(props.linkId)
            } else {
                comments = await commentsService.getCommentsByVideoId(props.videoId)
            }

            setComments(comments)

        } finally {
            setCommentsLoading(false)
        }
    }

    const addCommentAsync = async (comment: string, authorName?: string) => {
        try{
            setCommentsLoading(true)
            await commentsService.addCommentAsync(props.linkId!, comment, authorName)

            await loadCommentsAsync()
        } finally {
            setCommentsLoading(false)
        }
    }

    const deleteCommentAsync = async (commentId: string) => {
        // Implement delete comment logic
    }

    return (
        <CommentsContext.Provider
            value={{
                comments,
                commentsLoading,
                addCommentAsync,
                deleteCommentAsync,
                loadCommentsAsync
            }}>
            {
                props.allowAdding && <AddComment
                onAddClick={(comment, authorName) => addCommentAsync(comment, authorName)}/>
            }
            <div className="mt-5">
                <CommentsList/>
            </div>
        </CommentsContext.Provider>
    );
}

export default CommentsComponent;