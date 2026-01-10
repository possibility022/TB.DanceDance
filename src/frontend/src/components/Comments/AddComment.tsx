import React, {useContext, useState} from 'react';
import {AuthContext} from "../../providers/AuthProvider";

interface IAddCommentsProps {
    onAddClick(content: string, authorName?: string): void
}

function AddComment(props: IAddCommentsProps) {

    const maxLength = 2000
    const minLength = 3
    const maxNameLength = 20
    const minNameLength = 3
    const namePattern = /^[a-zA-Z0-9!@#$%^&*(){}[\] ]{3,20}$/
    const commentPattern = /^[a-zA-Z0-9!@#$%^&*(){}[\]\n ]{3,2000}$/

    const [errorMessage, setErrorMessage] = useState<string | undefined>()
    const [content, setContent] = useState('')
    const [nameForAnonymouse, setNameForAnonymouse] = useState<string>()
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

        if (content.match(commentPattern) === null) {
            setErrorMessage('Komentarz zawiera niedozwolone znaki')
            return
        }

        if (!authContext.isAuthenticated()) {
            if (nameForAnonymouse && nameForAnonymouse.length < minNameLength) {
                setErrorMessage('Podpis jest za krótki')
                return;
            }

            if (nameForAnonymouse && nameForAnonymouse.length > maxNameLength) {
                setErrorMessage('Podpis jest za długi')
                return
            }
        }

        if (content.match(namePattern) === null) {
            setErrorMessage('Podpis zawiera niedozwolone znaki')
        }

        setErrorMessage(undefined)

        props.onAddClick(content, nameForAnonymouse)
        setContent('')
    }

    function renderNameComponentWhenAnonymouse() {
        if (!authContext.isAuthenticated()) {
            return <>
                <div className="field">
                    <div className="control">
                        <input type="text"
                               className="input"
                               onChange={(e) => setNameForAnonymouse(e.target.value)}
                               value={nameForAnonymouse}
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
            {renderNameComponentWhenAnonymouse()}
            <div className="field">
                <div className="control">
                    <button className="button is-primary" onClick={onAdd}>Dodaj komentarz</button>
                </div>
            </div>
            {errorMessage && <p className="has-text-danger">{errorMessage}</p>}

        </>
    );
}

export default AddComment;