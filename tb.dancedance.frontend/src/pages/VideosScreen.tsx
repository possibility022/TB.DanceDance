import { Fragment, useEffect, useState } from 'react';
import { useNavigate } from 'react-router';
import { Button } from '../components/Button';
import { VideoList } from '../components/Videos/VideoList';
import { VideoInfoService } from '../services/VideoInfoService';
import VideoInformations from '../types/VideoInformations';


const videoService = new VideoInfoService()

export function VideoScreen() {

    const [videos, setVideos] = useState<Array<VideoInformations>>([]);
    const navigate = useNavigate()

    useEffect(() => {
        videoService.LoadVideos()
            .then(v => setVideos(v))
            .catch(e => console.log(e))

        return () => {
            // todo cleanup = abort request
        }
    }, []);

    return (
        <Fragment>
            <Button
                onClick={() => navigate('/video/upload')}>
                Wyslij Nagranie
            </Button>
            <VideoList videos={videos}></VideoList>
        </Fragment>)

}
