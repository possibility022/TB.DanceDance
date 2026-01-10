import React from 'react';
import {formatDateToPlDate} from "../../extensions/DateExtensions";

interface ICommentProps {
    author: string;
    content: string;
    lastUpdateDate: Date;
}

function Comment({ author, content, lastUpdateDate }: ICommentProps) {

    return (
        <article className="comment block">
            <div>
                <div>
                    <div>
                        <strong>{author}</strong>
                        <br />
                        <p>{content}</p>
                        <small className="has-text-grey">{formatDateToPlDate(lastUpdateDate)}</small>
                    </div>
                </div>
            </div>
        </article>
    );
}

export default Comment;