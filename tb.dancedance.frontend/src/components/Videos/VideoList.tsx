import VideoInformation from '../../types/VideoInformation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlayCircle } from '@fortawesome/free-regular-svg-icons'
import { useNavigate } from 'react-router-dom';
import { format } from 'date-fns'
import { pl } from 'date-fns/locale';

export interface ListOfVideos {
    videos: VideoInformation[]
}

const formatDate = (date: Date) => {
    // don't ask me
    // todo: find out why I need to cast it to string
    const d = Date.parse(date.toLocaleString())
    return format(d, 'dd MMMM yyyy', { locale: pl })
}

export function VideoList(props: ListOfVideos) {

    const navigate = useNavigate()

    const goToVideo = (vid: VideoInformation) => {
        const url = '/videos/' + vid.blobId
        navigate(url)
    }

    const list = props.videos.map(r => {
        return (
            <tr key={r.id}>
                <td>{r.name}</td>
                <td>{formatDate(r.recordedTimeUtc)}</td>
                <td>
                    <FontAwesomeIcon cursor={'pointer'} className='icon is-large' icon={faPlayCircle} onClick={() => goToVideo(r)} />
                </td>

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
