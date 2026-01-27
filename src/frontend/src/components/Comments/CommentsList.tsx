import React, {useContext} from 'react';
import Comment from "./Comment";
import {CommentsContext} from "./CommentsContext";
import AddComment from "./AddComment";

function CommentsList() {

    const commentsContext = useContext(CommentsContext)

    if (!commentsContext) {
        return <p>Error: Comments context not available</p>
    }

    if (!commentsContext.comments || commentsContext.comments?.length === 0) {
        return <p>Brak komentarzy</p>
    }

    return (
        <div>
            {commentsContext.comments!.map((comment, index) => (
                <Comment key={index}
                         author={comment.isAnonymous ? 'Anonymous' : comment.authorName!}
                         content={comment.content}
                         canDelete={comment.isOwn || comment.canModerate}
                         canHide={comment.canModerate}
                         canEdit={comment.isOwn}
                         lastUpdateDate={comment.updatedAt ?? comment.createdAt}/>
            ))}
        </div>
    );
}

export default CommentsList;