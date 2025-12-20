import VideoInformation from '../../types/ApiModels/VideoInformation';
import { useNavigate } from 'react-router-dom';
import { format } from 'date-fns'
import { pl } from 'date-fns/locale';
import { SharedScope } from '../../types/appTypes';
import { BlobId } from "../../types/ApiModels/TypeIds";
import {formatDateToPlDate} from "../../extensions/DateExtensions";

export interface ListOfVideos {
    videos: VideoInformation[]
    sharedScope?: SharedScope
    selectedVideo?: BlobId
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
    
    const getNameColumn = (title: string, isConverted: boolean) => {
        if (isConverted){
            return <td>{title}</td>
        } else {
            return <td>{title} - <p className="has-text-warning">Oczekuje na konwersje.</p></td>
        }
    }

    const list = props.videos.map(r => {

        return (
            <tr key={r.id} onClick={() => goToVideo(r)} className={getIsSelectedIndicator(r, props.selectedVideo)}>
                {getNameColumn(r.name, r.converted)}
                <td>{formatDateToPlDate(r.recordedDateTime)}</td>
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
