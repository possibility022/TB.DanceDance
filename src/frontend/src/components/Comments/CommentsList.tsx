import React, {useContext} from 'react';
import Comment from "./Comment";
import {CommentsContext} from "./CommentsContext";

function CommentsList() {

    const commentsContext = useContext(CommentsContext)

    if (!commentsContext) {
        return <p>Error: Comments context not available</p>
    }

    if (commentsContext.comments.length === 0 && commentsContext.commentsAvailable) {
        return <button className="button is-primary" onClick={commentsContext.loadCommentsAsync}>Wczytaj komentarze</button>
    }

    if (commentsContext.comments.length === 0) {
        return <p>Brak komentarzy</p>
    }

    return (
        <div>
            <h1 className="title is-size-4">Komentarze</h1>

            {commentsContext.comments.map((comment, index) => (
                <Comment key={index}
                         author={comment.isAnonymous ? 'Anonymous' : comment.authorName!}
                         content={comment.content}
                         lastUpdateDate={comment.updatedAt ?? comment.createdAt}/>
            ))}
        </div>
    );
}

export default CommentsList;