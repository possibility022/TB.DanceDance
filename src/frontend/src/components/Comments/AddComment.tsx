import React, {useContext, useState} from 'react';
import {AuthContext} from "../../providers/AuthProvider";

interface IAddCommentsProps {
    onAddCommentClick(content: string): void
    onAddAsAnonymousClick(content: string, authorName?: string): void
    onCancel?: () => void
    initialContent?: string
    initialAuthorName?: string
}

function AddComment(props: IAddCommentsProps) {

    const maxLength = 2000
    const minLength = 3
    const maxNameLength = 20
    const minNameLength = 3

    const buttonText = (props.onCancel && props.initialContent ? 'Edytuj' : 'Dodaj')
    const [errorMessage, setErrorMessage] = useState<string | undefined>()
    const [content, setContent] = useState(props.initialContent ?? '')
    const [nameForAnonymous, setNameForAnonymous] = useState<string>(props.initialAuthorName ?? '')
    const authContext = useContext(AuthContext)

    function onAdd() {
        if (content.length > maxLength) {
            setErrorMessage('Komentarz jest za długi')
            return
        }

        if (content.length < minLength) {
            setErrorMessage('Komentarz jest za krótki')
            return
        }

        const isAuthenticated = authContext.isAuthenticated()

        if (!isAuthenticated) {
            if (!nameForAnonymous || nameForAnonymous.length < minNameLength) {
                setErrorMessage('Podpis jest za krótki')
                return;
            }

            if (nameForAnonymous.length > maxNameLength) {
                setErrorMessage('Podpis jest za długi')
                return
            }
        }

        setErrorMessage(undefined)

        if (isAuthenticated) {
            props.onAddCommentClick(content)
        } else {
            props.onAddAsAnonymousClick(content, nameForAnonymous)
        }

        setContent('')
    }

    function renderNameComponentWhenAnonymous() {
        if (!authContext.isAuthenticated()) {
            return <>
                <div className="field">
                    <div className="control">
                        <input type="text"
                               className="input"
                               onChange={(e) => setNameForAnonymous(e.target.value)}
                               placeholder="Podpisz się"/>
                    </div>
                </div>
            </>
        }
    }

    return (
        <>
            <div className="field">
                <label className="label">Komentarz</label>
                <div className="control">
                    <textarea value={content}
                              onChange={(e) => setContent(e.target.value)}
                              className="textarea"
                              name="commentContent"
                              placeholder="Ale fajnie tańczysz!"/>
                </div>
            </div>
            {renderNameComponentWhenAnonymous()}
            <div className="field">
                <div className="control">
                    <button className="button is-primary" onClick={onAdd}>{buttonText}</button>
                    {props.onCancel && <button className="button is-link ml-1" onClick={props.onCancel}>Anuluj</button>}
                </div>
            </div>
            {errorMessage && <p className="has-text-danger">{errorMessage}</p>}

        </>
    );
}

export default AddComment;