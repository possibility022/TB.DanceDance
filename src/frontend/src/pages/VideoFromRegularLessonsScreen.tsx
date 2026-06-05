import {Fragment, JSX, useEffect, useState} from 'react';
import { useNavigate } from 'react-router';
import { Button } from '../components/Button';
import { VideoList } from '../components/Videos/VideoList';
import { VideoInfoService } from '../services/VideoInfoService';
import { VideoFromGroupInformation } from '../types/ApiModels/dancedance/apiModels';
import {formatDateToYearOnly} from "../extensions/DateExtensions";


type GroupInfo = {
    groupId: string
    groupName: string
    seasonStart?: Date
    seasonEnd?: Date
    videos: VideoFromGroupInformation[]
}

const videoService = new VideoInfoService()

export function VideoFromRegularLessonsScreen() {

    const [groups, setGroups] = useState<GroupInfo[]>([])
    const [videos, setVideos] = useState<VideoFromGroupInformation[]>([])
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [weHaveAnyVideos, setWeHaveAnyVideos] = useState<boolean>(true)
    const [activeGroup, setActiveGroup] = useState<string | null>(null)
    const [renderedList, setRenderedList] = useState<JSX.Element[]>([])

    const navigate = useNavigate()

    useEffect(() => {
        setIsLoading(true)

        Promise.all([
            videoService.GetVideosFromGroups(),
            videoService.GetUserEventsAndGroups()
        ])
            .then(([flatVideos, userAccess]) => {
                const allGroups = [
                    ...(userAccess.assigned?.groups ?? []),
                    ...(userAccess.available?.groups ?? [])
                ]
                const seasonMap = new Map(allGroups.map(g => [g.id!, g]))

                const grouped = new Map<string, GroupInfo>()
                for (const video of flatVideos) {
                    if (!video.groupId) continue
                    if (!grouped.has(video.groupId)) {
                        const seasonInfo = seasonMap.get(video.groupId)
                        grouped.set(video.groupId, {
                            groupId: video.groupId,
                            groupName: video.groupName ?? '',
                            seasonStart: seasonInfo?.seasonStart,
                            seasonEnd: seasonInfo?.seasonEnd,
                            videos: []
                        })
                    }
                    grouped.get(video.groupId)!.videos.push(video)
                }

                const groupsArray = Array.from(grouped.values())
                setGroups(groupsArray)
                setWeHaveAnyVideos(groupsArray.length > 0)
                if (groupsArray.length > 0) {
                    setActiveGroup(groupsArray[0].groupId)
                    setVideos(groupsArray[0].videos)
                    setRenderedList(renderListOfGroups(groupsArray, groupsArray[0].groupId))
                }
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
            const group = groups.find(r => r.groupId == activeGroup)
            setVideos(group?.videos ?? [])
            setRenderedList(renderListOfGroups(groups, activeGroup))
        }
    }, [activeGroup])

    const renderListOfGroups = (groups: GroupInfo[], activeGroupId: string) => {
        if (groups?.length > 0) {
            return groups.map(r => {
                const label = r.seasonStart && r.seasonEnd
                    ? `${r.groupName} (${formatDateToYearOnly(r.seasonStart)} - ${formatDateToYearOnly(r.seasonEnd)})`
                    : r.groupName
                return <li key={r.groupId} className={r.groupId == activeGroupId ? 'is-active' : ''}>
                    <a onClick={() => { setActiveGroup(r.groupId) }}>{label}</a>
                </li>
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
