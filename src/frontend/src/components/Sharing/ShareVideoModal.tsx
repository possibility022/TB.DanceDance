import React, {useEffect, useState} from 'react';
import axios, {AxiosResponse} from 'axios';
import VideoInformation from "../../types/ApiModels/VideoInformation";
import sharingService from "../../services/SharingService";
import SharedLinkResponse from "../../types/ApiModels/Sharing/SharedLinkResponse";
import ViewSharedLink from "./ViewSharedLink";


interface ShareVideoModalProps {
    onCloseClick(): void
    videoInfo: VideoInformation
}

function ShareVideoModal(props: ShareVideoModalProps) {

    const [errorMessage, setErrorMessage] = useState('')
    const [isSharingInProgress, setIsSharingInProgress] = useState(false)
    const [sharedLink, setSharedLink] = useState<SharedLinkResponse | null>(null)
    const abortControllerRef = React.useRef<AbortController | null>(null)

    useEffect(() => {
        return () => {
            abortControllerRef.current?.abort()
        }
    }, [])

    function generateLink() {
        if (!props.videoInfo)
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
                setErrorMessage('Some error occurred')
                console.error(error)
            })
            .finally(() => {
                setIsSharingInProgress(false)
            })
    }

    function renderContent(){
        if (sharedLink) {
            const link = window.location.origin + '/shared/' + sharedLink.linkId
            return <ViewSharedLink title={props.videoInfo.name} link={link}/>
        }
        else
            return <div className="content">
            <p>
                <strong>{props.videoInfo.name}</strong>
            </p>
            <p>
                Czy na pewno chcesz udostępnić to nagranie?<br />
                Utworzony link pozwoli wyświetlić wybrane nagranie każdej osobie, która go posiada. Wygenerowany link będzie działał przez 7 dni.
                Najlepiej udostępniać link tylko osobom, którym chcesz udostępnić nagranie.
            </p>
            <div className="buttons">
                <button className="button is-primary" onClick={generateLink}>
                    Udostępnij
                </button>
                <button className="button is-light" onClick={handleCancel}>
                    Anuluj
                </button>
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
                <article className="media">
                    <div className="media-content">
                        {renderContent()}
                    </div>
                </article>
            </div>
            </div>
            <button className="modal-close is-large" aria-label="close" onClick={handleCancel}></button>
        </div>
    );
}

export default ShareVideoModal;