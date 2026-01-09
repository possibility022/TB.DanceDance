import React, {useEffect, useState} from 'react';
import axios from 'axios';
import sharingService from "../../services/SharingService";
import videoInfoService from "../../services/VideoInfoService";
import SharedLinkResponse from "../../types/ApiModels/Sharing/SharedLinkResponse";
import ViewSharedLink from "./ViewSharedLink";
import VideoInformation from "../../types/ApiModels/VideoInformation";
import {CommentsVisibilityOptions} from "../../extensions/CommentsVisibilityOptions";

interface IShareVideoModalProps {
    videoInfo: VideoInformation
    onCloseClick: () => void
}

function ShareVideoModal(props: IShareVideoModalProps) {

    const [errorMessage, setErrorMessage] = useState<string>()
    const [isSharingInProgress, setIsSharingInProgress] = useState(false)
    const [updatingVisibilityInProgress, setUpdatingVisibilityInProgress] = useState(false)
    const [sharedLink, setSharedLink] = useState<SharedLinkResponse | null>(null)
    const abortControllerRef = React.useRef<AbortController | null>(null)
    const [allowComments, setAllowComments] = useState(true)
    const [allowCommentsAnonymous, setAllowCommentsAnonymous] = useState(false)
    const [selectedCommentsVisibility, setSelectedCommentsVisibility] = useState(props.videoInfo.commentVisibility.toString());
    const [currentVisibilityOptions, setCurrentVisibilityOptions] = useState(props.videoInfo.commentVisibility)

    useEffect(() => {
        return () => {
            abortControllerRef.current?.abort()
        }
    }, [])

    function changeCommentsVisibility() {
        if (updatingVisibilityInProgress)
            return

        setUpdatingVisibilityInProgress(true)
        videoInfoService.ChangeCommentsVisibility(props.videoInfo.id, Number.parseInt(selectedCommentsVisibility))
            .then(() => {
                setCurrentVisibilityOptions(Number.parseInt(selectedCommentsVisibility))
            })
            .catch(error => {setErrorMessage('Coś poszło nie tak.')})
        .finally(() => setUpdatingVisibilityInProgress(false))
    }

    function generateLink() {
        if (!props.videoInfo)
            return

        if (isSharingInProgress)
            return

        setIsSharingInProgress(true)

        if (abortControllerRef.current) {
            abortControllerRef.current.abort()
        }
        abortControllerRef.current = new AbortController()

        sharingService.shareVideo(props.videoInfo.id, undefined, abortControllerRef.current.signal)
            .then((res) => setSharedLink(res.data))
            .catch(error => {
                if (error.name === 'CanceledError' || error.name === 'AbortError' || axios.isCancel?.(error)) {
                    return
                }
                setErrorMessage('Coś poszło nie tak. Spróbuj ponownie.')
                console.error(error)
            })
            .finally(() => {
                setIsSharingInProgress(false)
            })
    }

    function submitShareButtonClasses() {
        if (isSharingInProgress)
            return 'button is-primary is-loading'
        else return 'button is-primary'
    }

    function submitVisibilityButtonClasses() {
        if (updatingVisibilityInProgress)
            return 'button is-warning is-loading'

        return 'button is-warning'
    }

    function renderListOfSharingOptions(){
        return Object.entries(CommentsVisibilityOptions).map(([key, value]) => (
            <option key={key} value={key}>
                {value}
            </option>
        ))
    }

    function renderContent() {
        if (sharedLink) {
            const link = window.location.origin + '/shared/' + sharedLink.linkId
            return <ViewSharedLink title={props.videoInfo.name} link={link}/>
        } else
            return <div className="content">
                <p>
                    <strong>Udostępniasz: {props.videoInfo.name}</strong>
                </p>
                <p>
                    Czy na pewno chcesz udostępnić to nagranie?<br/>
                    Utworzony link pozwoli wyświetlić wybrane nagranie każdej osobie, która go posiada. Wygenerowany
                    link będzie działał przez 7 dni.
                    Najlepiej udostępniać link tylko osobom, którym chcesz udostępnić nagranie.
                </p>
                <p className="has-text-danger">{errorMessage}</p>
                <div className="field">
                    <div className="control">
                        <label className="checkbox">
                            <input type="checkbox" name="allowComments" checked={allowComments}
                                   onChange={(e) => setAllowComments(e.target.checked)}/>
                            Zezwól na komentarze
                        </label>
                    </div>
                </div>
                <label className="checkbox">
                    <input type="checkbox" name="allowCommentsAnonymous"
                           checked={allowCommentsAnonymous && allowComments} disabled={!allowComments}
                           onChange={(e) => setAllowCommentsAnonymous(e.target.checked)}/>
                    Zezwól na komentarze także osobom niezalogowanym
                </label>

                <p className="has-text-warning">Obecne ustawienia widoczności komentarczy dla wybranego nagrania:</p>

                <div className="field is-grouped">
                    <p className="control">
                    <span className="select">
                      <select value={selectedCommentsVisibility}
                              onChange={(e) => setSelectedCommentsVisibility(e.target.value)}>
                          {renderListOfSharingOptions()}
                      </select>
                    </span>
                    </p>
                    <div className="control">
                        <button
                            className={submitVisibilityButtonClasses()}
                            disabled={selectedCommentsVisibility === currentVisibilityOptions.toString()}
                            onClick={changeCommentsVisibility}>
                            Zmien
                        </button>
                    </div>
                </div>

                <div className="field is-grouped">
                    <div className="control">
                        <button className={submitShareButtonClasses()} onClick={generateLink}>
                            Udostępnij
                        </button>
                    </div>
                    <div className="control">
                        <button className="button is-light" onClick={handleCancel}>
                            Anuluj
                        </button>
                    </div>
                </div>
            </div>
    }

    function handleCancel() {
        abortControllerRef.current?.abort()
        props.onCloseClick()
    }

    return (
        <div className="modal is-active">
            <div className="modal-background" onClick={handleCancel}></div>
            <div className="modal-content">
                <div className="box">
                    {renderContent()}
                </div>
            </div>
            <button className="modal-close is-large" aria-label="close" onClick={handleCancel}></button>
        </div>
    );
}

export default ShareVideoModal;