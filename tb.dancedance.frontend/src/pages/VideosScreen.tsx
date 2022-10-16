import { useEffect, useState } from 'react';
import { VideoList } from '../components/Videos/VideoList';
import { VideoInfoService } from '../services/VideoInfoService';
import VideoInformations from '../types/VideoInformations';


const videoService = new VideoInfoService()

export function VideoScreen() {

    const [videos, setVideos] = useState<Array<VideoInformations>>([]);

    useEffect(() => {
        videoService.LoadVideos()
            .then(v => setVideos(v))
            .catch(e => console.log(e))

        return () => {
            // todo cleanup = abort request
        }
    }, []);

    return (
        <VideoList videos={videos}></VideoList>
    )

}
