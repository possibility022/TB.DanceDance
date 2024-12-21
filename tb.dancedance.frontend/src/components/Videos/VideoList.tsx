import VideoInformation from '../../types/ApiModels/VideoInformation';
import { useNavigate } from 'react-router-dom';
import { format } from 'date-fns'
import { pl } from 'date-fns/locale';
import { SharedScope } from '../../types/appTypes';
import { BlobId } from "../../types/ApiModels/TypeIds";

export interface ListOfVideos {
    videos: VideoInformation[]
    sharedScope?: SharedScope
    selectedVideo?: BlobId
}

const formatDate = (date: Date) => {
    // don't ask me
    // todo: find out why I need to cast it to string
    const d = Date.parse(date.toLocaleString())
    return format(d, 'dd MMMM yyyy', { locale: pl })
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

    const list = props.videos.map(r => {

        return (
            <tr key={r.id} onClick={() => goToVideo(r)} className={getIsSelectedIndicator(r, props.selectedVideo)}>
                <td>{r.name}</td>
                <td>{formatDate(r.recordedDateTime)}</td>
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
