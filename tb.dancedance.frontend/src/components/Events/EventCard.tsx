import * as React from 'react';
import { Event } from '../../types/ApiModels/EventsAndGroups';
import { format } from 'date-fns'
import { pl } from 'date-fns/locale';
import './EventCard.css'

// eslint-disable-next-line @typescript-eslint/no-empty-interface
export interface IEventCardProps {
    event: Event
}

const formatDate = (date: Date) => {
    // don't ask me
    // todo: find out why I need to cast it to string
    const d = Date.parse(date.toLocaleString())
    return format(d, 'dd MMMM yyyy', { locale: pl })
}

export function EventCard(props: IEventCardProps) {
    return (
        <div key={props.event.id} className="card m-2 eventCard">
            <div className="card-content">
                <div className="columns is-vcentered">
                    <div className="column is-8">
                        <p className="bd-notification is-primary">{props.event.name}</p>
                    </div>
                    <div className="column">
                        <p className="bd-notification is-primary"><time>{formatDate(props.event.date)}</time></p>
                    </div>
                </div>
            </div>
        </div>
    );
}
