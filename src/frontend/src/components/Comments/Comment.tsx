import React from 'react';
import {formatDateToPlDate} from "../../extensions/DateExtensions";
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";
import {faTrash, faEdit, faBan} from "@fortawesome/free-solid-svg-icons";

interface ICommentProps {
    author: string;
    content: string;
    canDelete: boolean;
    canHide: boolean;
    canEdit: boolean;
    lastUpdateDate: Date;
}

function Comment(props: ICommentProps) {

    return (
        <article className="message">
            <div className="message-header">
                <strong>{props.author}</strong>
                <div>
                    { props.canHide && <button className="ml-4"><FontAwesomeIcon icon={faBan}/></button>}
                    { props.canEdit && <button className="ml-4"><FontAwesomeIcon icon={faEdit}/></button> }
                    { props.canDelete && <button className="ml-4"><FontAwesomeIcon className={"has-text-danger"} icon={faTrash}/></button> }
                </div>
            </div>
            <div className="message-body">
                <p>{props.content}</p>
                <small className="has-text-grey">{formatDateToPlDate(props.lastUpdateDate)}</small>
            </div>
        </article>
    )
        ;
}

export default Comment;