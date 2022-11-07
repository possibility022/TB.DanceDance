import ReactPlayer from 'react-player';
import { useParams } from 'react-router-dom';
import { AuthConsumer } from '../providers/AuthProvider';
import { IAuthService } from '../services/AuthService';
import { VideoInfoService } from '../services/VideoInfoService';


const videoService = new VideoInfoService()

export function VideoPlayerScreen() {

    const params = useParams();

    const constructUrl = (token: string | null) => {
        if (token && params.videoId) {
            // todo, improve authorization way
            const videoUrl = videoService.GetVideUrlByBlobId(params.videoId)
            return `${videoUrl}?token=${token}`
        }
        return ''
    }


    return <AuthConsumer>
        {({ getAccessToken }: IAuthService) => {
            return (
                <div className='container'>
                    <h4 className="title is-4">Triple step</h4>
                    <h5 className="title is-5">Triple step</h5>
                    <ReactPlayer
                        width='100%'
                        height='100%'
                        controls={true}
                        config={{ file: {attributes: {controlsList: 'nodownload'}} }}
                        // eslint-disable-next-line @typescript-eslint/no-unsafe-return, @typescript-eslint/no-unsafe-member-access, @typescript-eslint/no-unsafe-call
                        onContextMenu={(e: Event) => e.preventDefault()}

                        url={constructUrl(getAccessToken())

                        } />
                </div>
            )
        }}
    </AuthConsumer>


}