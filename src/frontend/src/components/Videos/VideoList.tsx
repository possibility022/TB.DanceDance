import VideoInformation from '../../types/ApiModels/VideoInformation';
import { useNavigate } from 'react-router-dom';
import { format } from 'date-fns'
import { pl } from 'date-fns/locale';
import { SharedScope } from '../../types/appTypes';
import { BlobId } from "../../types/ApiModels/TypeIds";
import {formatDateToPlDate} from "../../extensions/DateExtensions";
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";
import {faEdit, faShare} from "@fortawesome/free-solid-svg-icons";

export interface ListOfVideos {
    videos: VideoInformation[]
    sharedScope?: SharedScope
    selectedVideo?: BlobId
    enableShare: boolean
    onShareClick?: (video: VideoInformation) => void
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
                            if (props.onShareClick)
                                props.onShareClick(video)
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

    return (
        <table className="table is-striped is-hoverable is-fullwidth">
            <tbody>
                {list}
            </tbody>
        </table>
    );
}
