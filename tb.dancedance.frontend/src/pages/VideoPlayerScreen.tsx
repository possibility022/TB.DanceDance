import ReactPlayer from 'react-player';
import { useState, useContext, useEffect } from 'react'
import { useParams } from 'react-router-dom';
import { AuthContext } from '../providers/AuthProvider';
import videoInfoService from '../services/VideoInfoService';
import VideoInformation from '../types/VideoInformation';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCancel, faCheck, faEdit } from '@fortawesome/free-solid-svg-icons';


export function VideoPlayerScreen() {

    const params = useParams();
    const authContext = useContext(AuthContext)

    const [videoInfo, setVideoInfo] = useState<VideoInformation>()

    const [url, setUrl] = useState<string | undefined>()
    const [editIsHidden, setEditIsHidden] = useState(true)
    const [videoNameToSet, setVideoNameToSet] = useState('')


    useEffect(() => {

        const videoId = params.videoId as string

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
    }, [])


    function onEditClick() {
        setVideoNameToSet(videoInfo?.name ?? '')
        setEditIsHidden(!editIsHidden)
    }

    function onRenameConfirm() {
        if (videoInfo) {
            videoInfoService.RenameVideo(videoInfo.id.toString(), videoNameToSet) //todo, unify what to use to represent guids.
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
        </div>
    )



}