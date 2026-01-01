import React from 'react';
import ReactPlayer from "react-player";
import {VideoList} from "./VideoList";
import {BlobId} from "../../types/ApiModels/TypeIds";
import VideoInformation from "../../types/ApiModels/VideoInformation";
import {SharedScope} from "../../types/appTypes";

export interface VideoPlayerComponentProps {
    videoId: BlobId | undefined;
    sharedScope: SharedScope | undefined;
    videoList: VideoInformation[];
    url: string | undefined;
}

function VideoPlayerComponent(props: VideoPlayerComponentProps) {
    return (
        <div>
            <ReactPlayer
                width='100%'
                height='100%'
                controls={true}
                config={{ file: { attributes: { controlsList: 'nodownload' } } }}
                onContextMenu={(e: Event) => e.preventDefault()}

                url={props.url} />

            <VideoList videos={props.videoList}
                       sharedScope={props.sharedScope}
                       enableShare={false}
                       selectedVideo={props.videoId}></VideoList>
        </div>
    );
}

export default VideoPlayerComponent;