import {Fragment, JSX, useEffect, useState} from 'react';
import { useNavigate } from 'react-router';
import { Button } from '../components/Button';
import { VideoList } from '../components/Videos/VideoList';
import { VideoInfoService } from '../services/VideoInfoService';
import { IGroupWithVideosResponse } from '../types/ApiModels/GroupsWithVideosResponse';
import VideoInformation from '../types/ApiModels/VideoInformation';
import { GroupId } from "../types/ApiModels/TypeIds";
import {formatDateToPlDate, formatDateToYearOnly} from "../extensions/DateExtensions";


const videoService = new VideoInfoService()

export function VideoScreen() {

    const [groups, setGroups] = useState<Array<IGroupWithVideosResponse>>([])
    const [videos, setVideos] = useState<Array<VideoInformation>>([])
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [weHaveAnyVideos, setWeHaveAnyVideos] = useState<boolean>(true)
    const [activeGroup, setActiveGroup] = useState<GroupId | null>(null)
    const [renderedList, setRenderedList] = useState<Array<JSX.Element>>([])

    const navigate = useNavigate()

    useEffect(() => {
        setIsLoading(true)
        videoService.GetVideosFromGroups()
            .then(v => {
                setGroups(v)
                setWeHaveAnyVideos(v.length > 0)
                if (v.length > 0) {
                    setActiveGroup(v[0].groupId)
                }

                setRenderedList(renderListOfGroups(v, v[0].groupId))
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
                    void navigate('/videos/requestassignment')
                }}>tutaj</Button>
            </div>
    }

    useEffect(() => {
        if (activeGroup != null) {
            const vid = groups.find(r => r.groupId == activeGroup)?.videos ?? []
            setVideos(vid)
            setRenderedList(renderListOfGroups(groups, activeGroup))
        }
    }, [activeGroup])

    const renderListOfGroups = (groups: Array<IGroupWithVideosResponse>, activeGroup: string) => {
        if (groups?.length > 0) {
            return groups.map(r => {
                return <li key={r.groupId} className={r.groupId == activeGroup ? 'is-active' : ''}><a onClick={() => {setActiveGroup(r.groupId)}}>{r.groupName} ({formatDateToYearOnly(r.seasonStart)} - {formatDateToYearOnly(r.seasonEnd)})</a></li>
            })
        }
        return []
    }



    return (
        <div className='container mt-6'>
            <div className="tabs">
                <ul>
                    {renderedList}
                </ul>
            </div>
            <Button
                onClick={() => void navigate('/videos/upload')}>
                Wyslij Nagranie
            </Button>
            <VideoList videos={videos}
                       enableShare={false}
                       sharedScope={{
                eventId: null,
                groupId: activeGroup
            }}/>

            {loadingBar()}
            {askForVideos()}
        </div>)

}
