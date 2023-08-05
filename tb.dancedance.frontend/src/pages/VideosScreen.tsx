import { Fragment, useEffect, useState } from 'react';
import { useNavigate } from 'react-router';
import { Button } from '../components/Button';
import { VideoList } from '../components/Videos/VideoList';
import { VideoInfoService } from '../services/VideoInfoService';
import VideoInformation from '../types/VideoInformation';


const videoService = new VideoInfoService()

export function VideoScreen() {

    const [videos, setVideos] = useState<Array<VideoInformation>>([]);
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [weHaveAnyVideos, setWeHaveAnyVideos] = useState<boolean>(true)

    const navigate = useNavigate()

    useEffect(() => {
        setIsLoading(true)
        videoService.GetVideosFromGroups()
            .then(v => {
                setVideos(v)
                setWeHaveAnyVideos(v.length > 0)

            })
            .catch(e => console.log(e))
            .finally(() => setIsLoading(false))

        return () => {
            // todo cleanup = abort request
        }
    }, []);

    const loadingBar = () => {
        if (isLoading)
            return <progress className="progress is-large is-info" max="100">60%</progress>
    }

    const askForVideos = () => {
        if (!weHaveAnyVideos)
            return <div>
                <h3>Wygląda na to, że nie masz dostępu do nagrań.</h3>
                Możesz poprosić o dostęp klikając <Button onClick={() => {
                    navigate('/videos/requestassignment')
                }}>tutaj</Button>
            </div>
    }

    return (
        <Fragment>
            <Button
                onClick={() => navigate('/videos/upload')}>
                Wyslij Nagranie
            </Button>
            <VideoList videos={videos}></VideoList>
            {loadingBar()}
            {askForVideos()}
        </Fragment>)

}
