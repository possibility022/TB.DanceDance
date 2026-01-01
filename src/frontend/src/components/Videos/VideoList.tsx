import VideoInformation from '../../types/ApiModels/VideoInformation';
import { useNavigate } from 'react-router-dom';
import { SharedScope } from '../../types/appTypes';
import { BlobId } from "../../types/ApiModels/TypeIds";
import {formatDateToPlDate} from "../../extensions/DateExtensions";
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";
import {faShare} from "@fortawesome/free-solid-svg-icons";
import React, {useState} from "react";
import ShareVideoModal from "../Sharing/ShareVideoModal";

export interface ListOfVideos {
    videos: VideoInformation[]
    sharedScope?: SharedScope
    selectedVideo?: BlobId
    enableShare: boolean
}

const getIsSelectedIndicator = (videoInfo: VideoInformation, selected?: BlobId)=>
{
    if (videoInfo == null || selected == null)
        return ''
    if (videoInfo.blobId == selected)
        return 'is-selected'
    return  ''
}

export function VideoList(props: ListOfVideos) {

    const navigate = useNavigate()

    const [showShareModal, setShowShareModal] = useState<{
        videoInformation?: VideoInformation,
        show: boolean
    }>({videoInformation: undefined, show: false})

    const renderShareModal = () => {
        if (showShareModal.show)
            return <ShareVideoModal videoInfo={showShareModal.videoInformation!}
                                    onCloseClick={() => setShowShareModal({
                                        videoInformation: undefined,
                                        show: false
                                    })}></ShareVideoModal>
        else
            return <></>
    }

    const goToVideo = (vid: VideoInformation) => {
        if (!vid.converted)
            return
        
        const url = '/videos/' + vid.blobId
        void navigate(url, { state: props.sharedScope })
    }
    
    const getNameColumn = (vid: VideoInformation) => {
        if (vid.converted){
            return <td>{vid.name}</td>
        } else {
            return <td>{vid.name} <p className="has-text-warning">Oczekuje na konwersje.</p></td>
        }
    }

    const getShareColumn = (video: VideoInformation) => {
        if (props.enableShare) {
            return <td>
                        <span className="icon m-1 has-text-info" onClick=
                            {(event) => {
                            event.stopPropagation();
                            setShowShareModal({videoInformation: video, show: true})
                        }}>
                        <FontAwesomeIcon icon={faShare}/>
                    </span>
            </td>
        } else {
            return <td></td>
        }
    }

    const list = props.videos.map(video => {

        return (
            <tr key={video.id} onClick={() => goToVideo(video)} className={getIsSelectedIndicator(video, props.selectedVideo)}>
                {getNameColumn(video)}
                <td>{formatDateToPlDate(video.recordedDateTime)}</td>
                {getShareColumn(video)}
            </tr>
        )
    })

    return <>
        { renderShareModal() }
        <table className="table is-striped is-hoverable is-fullwidth">
            <tbody>
                {list}
            </tbody>
        </table>
    </>
}
