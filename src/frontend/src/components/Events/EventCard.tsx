import * as React from 'react';
import { Event } from '../../types/ApiModels/EventsAndGroups';
import { format } from 'date-fns'
import { pl } from 'date-fns/locale';
import './EventCard.css'
import { VideoList } from '../Videos/VideoList';
import VideoInformation from '../../types/ApiModels/VideoInformation';
import videoInfoService from '../../services/VideoInfoService';
import { Button } from '../Button';

export interface IEventCardProps {
    event: Event
    onSelected(id: string): void
    onUploadClick(id: string): void
}

const formatDate = (date: Date) => {
    // don't ask me
    // todo: find out why I need to cast it to string
    const d = Date.parse(date.toLocaleString())
    return format(d, 'dd MMMM yyyy', { locale: pl })
}

export function EventCard(props: IEventCardProps) {


    const [contentIsHidden, setContentIsHidden] = React.useState(true)
    const [videos, setVideos] = React.useState<Array<VideoInformation>>([])
    const [isLoading, setIsLoading] = React.useState(true)

    const loadContent = () => {
        const isHidden = !contentIsHidden
        setContentIsHidden(!contentIsHidden)

        if (isHidden == true)
            return

        setIsLoading(true)

        videoInfoService.GetVideosForEvent(props.event.id)
            .then((results) => {
                setVideos(results)
            })
            .catch(e => console.error(e))
            .finally(() => {
                setIsLoading(false)
            })
    }

    const progressBar = () => {
        if (isLoading)
            return <progress className="progress is-small is-primary" max="100"></progress>
    }

    return (
        <div key={props.event.id} className="card m-2" onClick={() => props.onSelected(props.event.id)}>
            <header className="card-header eventCard" onClick={loadContent}>
                <p className="card-header-title">
                    {props.event.name}
                </p>
                <button className="card-header-icon" aria-label="more options">
                    <span className="bd-notification is-primary">
                        <p className="bd-notification is-primary"><time>{formatDate(props.event.date)}</time></p>
                    </span>
                </button>
            </header>
            <div className="card-content" hidden={contentIsHidden}>
                {progressBar()}
                <VideoList videos={videos}
                           enableShare={false}
                           sharedScope={{
                    groupId: null,
                    eventId: props.event.id,
                }}/>
            </div>
            <div hidden={contentIsHidden}>
                <footer className="card-footer">
                    <p className="card-footer-item">
                        <Button onClick={() => props.onUploadClick(props.event.id)}>Wyślij nagranie</Button>
                    </p>
                </footer>
            </div>
        </div>
    );
}
