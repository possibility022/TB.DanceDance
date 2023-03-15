import ReactPlayer from 'react-player';
import { useState, useContext, useEffect } from 'react'
import { useParams } from 'react-router-dom';
import { AuthContext } from '../providers/AuthProvider';
import { VideoInfoService } from '../services/VideoInfoService';
import VideoInformation from '../types/VideoInformation';


const videoService = new VideoInfoService()

export function VideoPlayerScreen() {

    const params = useParams();
    const authContext = useContext(AuthContext)

    const [videoInfo, setVideoInfo] = useState<VideoInformation>()

    const [url, setUrl] = useState<string | undefined>()


    useEffect(() => {

        const videoId = params.videoId as string

        authContext.getAccessToken()
            .then((token) => {
                if (token && videoId) {
                    // todo, improve authorization way
                    const videoUrl = videoService.GetVideUrlByBlobId(videoId)
                    setUrl(`${videoUrl}?token=${token}`)
                }
            })
            .catch(e => console.error(e))

        videoService.GetVideoInfo(videoId)
            .then(videoInfo => {
                setVideoInfo(videoInfo)
            })
            .catch(e => console.error(e))

        return () => {
            // todo, cleanup
        }
    }, [])



    return (
        <div className='container'>
            <h4 className="title is-5">{videoInfo?.name}</h4>
            
            <ReactPlayer
                width='100%'
                height='100%'
                controls={true}
                config={{ file: { attributes: { controlsList: 'nodownload' } } }}
                // eslint-disable-next-line @typescript-eslint/no-unsafe-return, @typescript-eslint/no-unsafe-member-access, @typescript-eslint/no-unsafe-call
                onContextMenu={(e: Event) => e.preventDefault()}

                url={url} />
        </div>
    )



}