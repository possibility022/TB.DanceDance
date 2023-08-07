import * as React from 'react';
import { Event } from '../../types/ApiModels/EventsAndGroups';
import { EventCard } from './EventCard';

export interface IEventsListProps {
    events: Array<Event>
    onSelected(id: string): void
    onUploadClick(id: string): void
}

export function EventsList(props: IEventsListProps) {

    const list = props.events.map(event => {
        return <EventCard
            key={event.id} 
            event={event}
            onSelected={(id) => props.onSelected(id)}
            onUploadClick={(id) => props.onUploadClick(id)} />
    })

    return (
        <div>
            {list}
        </div>
    );
}

