import * as React from 'react';
import { Button } from '../Button';
import { IEventBase } from '../../types/ApiModels/EventsAndGroups';
import { EventType } from '../../types/EventType';

export interface ICreateNewEventProps {
    onCancel(): void
    onSubmit(newEvent: IEventBase): void
}

export function CreateNewEvent(props: ICreateNewEventProps) {

    const [name, setName] = React.useState("")
    const [date, setDate] = React.useState("")

    const getEvent = (): IEventBase => {
        return {
            date: new Date(Date.parse(date)),
            name: name,
            eventType: EventType.PointedEvent
        }
    }

    return (
        <div className='box'>
            <h1 className="title">Nowe wydarzenie</h1>
            <div className="field">
                <label className="label">Nazwa</label>
                <div className="control">
                    <input className="input" type="text" value={name} onChange={(v) => setName(v.target.value)} placeholder="Swing Fiction 2023" />
                </div>
            </div>

            <div className="field">
                <label className="label">Data</label>
                <div className="control">
                    <input className="input" type="date" value={date} onChange={(v) => setDate(v.target.value)} placeholder='20.1.2023' />
                </div>
            </div>

            <div className="field is-grouped">
                <div className="control">
                    <Button classNames='is-primary' onClick={() => props.onSubmit(getEvent())}>Dodaj</Button>
                </div>

                <div className='control'>
                    <Button onClick={() => props.onCancel()}>Anuluj</Button>
                </div>
            </div>
        </div>
    );
}
