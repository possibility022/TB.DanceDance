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

    async function loadCommentsAsync() {
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

    async function addCommentAsync(comment: string) {
        try{
            setCommentsLoading(true)
            await commentsService.addCommentAsync(props.linkId!, comment)

            await loadCommentsAsync()
        } finally {
            setCommentsLoading(false)
        }
    }

    async function addCommentAsAnonymouseAsync(comment: string, authorName: string) {
        try{
            setCommentsLoading(true)
            await commentsService.addCommentAsAnonymouseAsync(props.linkId!, comment, authorName)

            await loadCommentsAsync()
        } finally {
            setCommentsLoading(false)
        }
    }

    async function editCommentAsync(commentId: string, newContent: string) {
        await commentsService.editCommentAsync(commentId, newContent)
    }

    async function deleteCommentAsync(commentId: string) {
        try {
            setCommentsLoading(true)
            await commentsService.deleteCommentAsync(commentId)
            await loadCommentsAsync()
        } finally {
            setCommentsLoading(false)
        }
    }

    async function hideCommentAsync(commentId: string, hide: boolean) {
        try {
            setCommentsLoading(true)
            if (hide)
                await commentsService.hideCommentAsync(commentId)
            else
                await commentsService.unHideCommentAsync(commentId)
            await loadCommentsAsync()
        } finally {
            setCommentsLoading(false)
        }
    }

    function reportCommentAsync(commentId: string) : Promise<void> {
        return Promise.resolve()
    }

    return (
        <CommentsContext.Provider
            value={{
                comments,
                commentsLoading,
                addCommentAsync,
                addCommentAsAnonymouseAsync,
                loadCommentsAsync,
                hideCommentAsync,
                editCommentAsync,
                reportCommentAsync,
                deleteCommentAsync,
            }}>
            {
                props.allowAdding && <AddComment
                onAddCommentClick={addCommentAsync}
                onAddAsAnonymouseClick={addCommentAsAnonymouseAsync}
                />
            }
            <div className="mt-5">
                <CommentsList/>
            </div>
        </CommentsContext.Provider>
    );
}

export default CommentsComponent;