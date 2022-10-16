import * as React from 'react';
import VideoInformations from '../../types/VideoInformations';
import { IonIcon } from "react-ion-icon";
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
                        onClick={e => goToVideo(r)}
                        content={<IonIcon size='large' name="play-outline" />} />
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
