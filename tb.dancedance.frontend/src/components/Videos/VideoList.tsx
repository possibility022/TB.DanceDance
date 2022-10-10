import * as React from 'react';
import VideoInformations from '../../types/VideoInformations';

export interface ListOfVideos {
    Videos: VideoInformations[]
}

export function VideoList(props: ListOfVideos) {

    const list = props.Videos.map(r => {
        return (
            <tr key={r.id}>
                <td>{r.name}</td>
                <td>{r.creationTimeUtc.toLocaleString()}</td>
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
