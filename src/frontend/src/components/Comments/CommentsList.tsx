import React, {useContext} from 'react';
import Comment from "./Comment";
import {CommentsContext} from "./CommentsContext";

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
                         onReportAsync={(why) => commentsContext.reportCommentAsync(comment.id, why)}
                         onDeleteAsync={() => commentsContext.deleteCommentAsync(comment.id)}
                         onEditAsync={(newContent, authorName) => commentsContext.editCommentAsync(comment.id, newContent, authorName)}
                         onHideSwitchAsync={(hide) => commentsContext.hideCommentAsync(comment.id, hide)}
                         comment={comment}/>
            ))}
        </div>
    );
}

export default CommentsList;