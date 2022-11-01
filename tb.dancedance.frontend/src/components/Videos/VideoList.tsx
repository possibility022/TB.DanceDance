import * as React from 'react';
import VideoInformations from '../../types/VideoInformations';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faPlayCircle } from '@fortawesome/free-regular-svg-icons'
import { Button } from '../Button';
import { useNavigate } from 'react-router-dom';

export interface ListOfVideos {
    videos: VideoInformations[]
}

export function VideoList(props: ListOfVideos) {

    const navigate = useNavigate()

    const goToVideo = (vid: VideoInformations) => {
        const url = '/videos/' + vid.blobId
        navigate(url)
    }

    const list = props.videos.map(r => {
        return (
            <tr key={r.id}>
                <td>{r.name}</td>
                <td>{r.creationTimeUtc.toLocaleString()}</td>
                <td>
                    <Button
                        onClick={() => goToVideo(r)}
                        content={<FontAwesomeIcon icon={faPlayCircle} />} />
                </td>

            </tr>
        )
    })

    return (
        <table className="table">
            <tbody>
                {list}
            </tbody>
        </table>
    );
}
