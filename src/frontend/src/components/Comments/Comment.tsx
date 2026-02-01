import React, {useState} from 'react';
import {formatDateToPlDate} from "../../extensions/DateExtensions";
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";
import {faTrash, faEdit, faEyeSlash, faFontAwesomeFlag} from "@fortawesome/free-solid-svg-icons";
import {CommentResponse} from "../../types/ApiModels/Comments/CommentResponse";
import AddComment from "./AddComment";

interface ICommentProps {
    comment: CommentResponse
    canReport?: boolean
    onDeleteAsync: () => Promise<void>
    onHideSwitchAsync: (hide: boolean) => Promise<void>
    onEditAsync: (newContent: string) => Promise<void>
    onReportAsync: (reason: string) => Promise<void>
}

const canDelete = (comment: CommentResponse) => comment.isOwn || comment.canModerate
const canHide = (comment: CommentResponse) => comment.canModerate
const canEdit = (comment: CommentResponse) => comment.isOwn

function Comment(props: ICommentProps) {

    const [inEditMode, setInEditMode] = useState(false)

    function openReportModal() {
        props.onReportAsync('')
    }

    function enableEditing() {
        setInEditMode(true)
    }

    function renderComment() {

        const onEdit = (newContent: string) => {
            props.onEditAsync(newContent)
            setInEditMode(false)
        }

        if (inEditMode) {
            return <AddComment onAddCommentClick={onEdit}
                               initialContent={props.comment.content}
                               onCancel={() => setInEditMode(false)}
                               onAddAsAnonymouseClick={onEdit}/>
        } else {
            return <p>{props.comment.content}</p>
        }
    }

    function renderHideTag() {

        if (canHide(props.comment)) {
            if (props.comment.isHidden) {
                return <span onClick={() => props.onHideSwitchAsync(false)} className="tag ml-4 is-warning">Ukryty</span>
            } else {
                return <button className="ml-4 is-warning" onClick={() => props.onHideSwitchAsync(true)}>
                    <FontAwesomeIcon icon={faEyeSlash}/>
                </button>
            }
        }
    }

    return (
        <article className="message">
            <div className="message-header">
                <strong>{props.comment.isAnonymous ? 'Anonymous' : props.comment.authorName!}</strong>
                <div>
                    {props.canReport && <button className="ml-4"
                                                onClick={() => openReportModal()}><FontAwesomeIcon
                        icon={faFontAwesomeFlag}/></button>}
                    {renderHideTag()}
                    {canEdit(props.comment) &&
                        <button className="ml-4" onClick={enableEditing}><FontAwesomeIcon
                            icon={faEdit}/></button>}
                    {canDelete(props.comment) &&
                        <button className="ml-4" onClick={props.onDeleteAsync}><FontAwesomeIcon
                            className={"has-text-danger"}
                            icon={faTrash}/>
                        </button>}
                </div>
            </div>
            <div className="message-body">
                {renderComment()}
                <small
                    className="has-text-grey">{formatDateToPlDate(props.comment.updatedAt ?? props.comment.createdAt)}</small>
            </div>
        </article>
    )
        ;
}

export default Comment;