import ReactPlayer from 'react-player';
import { useState, useContext, useEffect } from 'react'
import { useLocation, useParams } from 'react-router-dom';
import { AuthContext } from '../providers/AuthProvider';
import videoInfoService from '../services/VideoInfoService';
import VideoInformation from '../types/ApiModels/VideoInformation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCancel, faCheck, faEdit } from '@fortawesome/free-solid-svg-icons';
import { SharedScope } from '../types/appTypes';
import { VideoList } from '../components/Videos/VideoList';



export function VideoPlayerScreen() {

    const params = useParams();
    const location = useLocation()
    const authContext = useContext(AuthContext)

    const [videoInfo, setVideoInfo] = useState<VideoInformation>()

    const [url, setUrl] = useState<string | undefined>()
    const [editIsHidden, setEditIsHidden] = useState(true)
    const [videoNameToSet, setVideoNameToSet] = useState('')
    const [videoList, setVideoList] = useState<VideoInformation[]>([])
    const [sharedScope, setSharedScope] = useState<SharedScope>()

    useEffect(() => {

        const videoId = params.videoId as string

        if (videoId == videoInfo?.id)
            return

        const passedSharedScope = location.state as SharedScope

        if (passedSharedScope) {
            setSharedScope(passedSharedScope)

            if (passedSharedScope.groupId) {
                videoInfoService.GetVideosForGroup(passedSharedScope.groupId)
                    .then(videos => {
                        setVideoList(videos.videos)
                    })
                    .catch(e => console.log(e))
            } else if (passedSharedScope.eventId) {
                videoInfoService.GetVideosForEvent(passedSharedScope.eventId)
                    .then(videos => {
                        setVideoList(videos)
                    }).catch(e => console.log(e))
            }
        }

        authContext.getAccessToken()
            .then((token) => {
                if (token && videoId) {
                    // todo, improve authorization way
                    const videoUrl = videoInfoService.GetVideUrlByBlobId(videoId)
                    setUrl(`${videoUrl}?token=${token}`)
                }
            })
            .catch(e => console.error(e))

        videoInfoService.GetVideoInfo(videoId)
            .then(videoInfo => {
                setVideoInfo(videoInfo)
            })
            .catch(e => console.error(e))

        return () => {
            // todo, cleanup
        }
    }, [params])

    function onEditClick() {
        setVideoNameToSet(videoInfo?.name ?? '')
        setEditIsHidden(!editIsHidden)
    }

    function onRenameConfirm() {
        if (videoInfo) {
            videoInfoService.RenameVideo(videoInfo.id, videoNameToSet)
                .then(results => {
                    if (results) {
                        setVideoInfo({
                            ...videoInfo,
                            name: videoNameToSet
                        })
                    }
                }).finally(() => {
                    setEditIsHidden(true)
                })
        }
    }

    function onRenameCancel() {
        setEditIsHidden(true)
    }


    return (
        <div className='container'>
            <div hidden={editIsHidden}>
                <input className="input" type="text" placeholder="Korki podstawowe"
                    value={videoNameToSet}
                    onChange={(e) => setVideoNameToSet(e.target.value)}
                />
                <span className="icon m-1" onClick={onRenameConfirm}>
                    <FontAwesomeIcon icon={faCheck} />
                </span>
                <span className="icon m-1" onClick={onRenameCancel}>
                    <FontAwesomeIcon icon={faCancel} />
                </span>
            </div>

            <div hidden={!editIsHidden}>
                <h4 className="title is-5">{videoInfo?.name}
                    <span className="icon m-1" onClick={onEditClick}>
                        <FontAwesomeIcon icon={faEdit} />
                    </span>
                </h4>
            </div>

            <ReactPlayer
                width='100%'
                height='100%'
                controls={true}
                config={{ file: { attributes: { controlsList: 'nodownload' } } }}
                // eslint-disable-next-line @typescript-eslint/no-unsafe-return, @typescript-eslint/no-unsafe-member-access, @typescript-eslint/no-unsafe-call
                onContextMenu={(e: Event) => e.preventDefault()}

                url={url} />

            <VideoList videos={videoList} sharedScope={sharedScope}></VideoList>
        </div>
    )



}