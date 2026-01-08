import React from 'react';

interface ICommentProps {
    author: string;
    content: string;
    lastUpdateDate: Date;
}

function Comment({ author, content, lastUpdateDate }: ICommentProps) {
    const formattedDate = lastUpdateDate.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });

    return (
        <article className="comment block">
            <div>
                <div>
                    <div>
                        <strong>{author}</strong>
                        <br />
                        <p>{content}</p>
                        <small className="has-text-grey">{formattedDate}</small>
                    </div>
                </div>
            </div>
        </article>
    );
}

export default Comment;