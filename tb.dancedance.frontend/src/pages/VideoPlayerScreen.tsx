import ReactPlayer from 'react-player';
import { useState, useContext, useEffect } from 'react'
import { useParams } from 'react-router-dom';
import { AuthContext } from '../providers/AuthProvider';
import { VideoInfoService } from '../services/VideoInfoService';


const videoService = new VideoInfoService()

export function VideoPlayerScreen() {

    const params = useParams();
    const authContext = useContext(AuthContext)

    const [url, setUrl] = useState<string | undefined>()


    useEffect(() => {
        authContext.getAccessToken()
            .then((token) => {
                if (token && params.videoId) {
                    // todo, improve authorization way
                    const videoUrl = videoService.GetVideUrlByBlobId(params.videoId)
                    setUrl(`${videoUrl}?token=${token}`)
                }
            })
            .catch(e => console.log(e))

        return () => {
            // todo, cleanup
        }
    }, [])



    return (
        <div className='container'>
            <h4 className="title is-4">Triple step</h4>
            <h5 className="title is-5">Triple step</h5>
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