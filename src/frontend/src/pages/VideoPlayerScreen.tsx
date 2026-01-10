import { useState, useContext, useEffect } from 'react'
import { useLocation, useParams } from 'react-router-dom';
import { AuthContext } from '../providers/AuthProvider';
import videoInfoService from '../services/VideoInfoService';
import VideoInformation from '../types/ApiModels/VideoInformation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCancel, faCheck, faEdit } from '@fortawesome/free-solid-svg-icons';
import { SharedScope } from '../types/appTypes';
import {BlobId} from "../types/ApiModels/TypeIds";
import VideoPlayerComponent from "../components/Videos/VideoPlayerComponent";
import CommentsComponent from "../components/Comments/CommentsComponent";



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
    
    const useEffectAsyncBody = async () => {
        const videoId = params.videoId as BlobId

        if (videoId == videoInfo?.id)
            return

        const passedSharedScope = location.state as SharedScope

        if (passedSharedScope) {
            setSharedScope(passedSharedScope)
            let videos: VideoInformation[] = [];

            if (passedSharedScope.groupId) {
                const groupWithVideos = await videoInfoService.GetVideosForGroup(passedSharedScope.groupId)
                videos = groupWithVideos.videos
            } else if (passedSharedScope.eventId) {
                videos = await videoInfoService.GetVideosForEvent(passedSharedScope.eventId)
            }
            
            setVideoList(videos)
        }

        const token = await authContext.getAccessToken();
        if (token && videoId) {
            // todo, improve authorization way
            const videoUrl = videoInfoService.GetVideUrlByBlobId(videoId)
            const newUrl = `${videoUrl}?token=${token}`;
            if (newUrl !== url) {
                setUrl(newUrl);
            }
        }

        const vidInfo = await videoInfoService.GetVideoInfo(videoId)
        setVideoInfo(vidInfo)
    }

    useEffect(() => {
        
        useEffectAsyncBody()
            .catch(e => console.log(e))
        
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
            void videoInfoService.RenameVideo(videoInfo.id, videoNameToSet)
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

            <VideoPlayerComponent videoId={params.videoId} sharedScope={sharedScope} videoList={videoList} url={url}/>
            {videoInfo && <CommentsComponent allowAdding={false} videoId={videoInfo.id}/>}
        </div>
    )



}