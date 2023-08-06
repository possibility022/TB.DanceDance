import * as React from 'react';
import { Button } from '../Button';
import { IEventBase } from '../../types/ApiModels/EventsAndGroups';
import { EventType } from '../../types/EventType';

export interface IUploadVideoModalProps {
    onCancel(): void
    onSubmit(): void
}

export function UploadVideoModal(props: IUploadVideoModalProps) {

    const [name, setName] = React.useState("")
    const [date, setDate] = React.useState("")

    return (
        <div className='container has-background-white p-6'>
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
                    <Button classNames='is-primary' onClick={() => props.onSubmit()}>Dodaj</Button>
                </div>

                <div className='control'>
                    <Button onClick={() => props.onCancel()}>Anuluj</Button>
                </div>
            </div>
        </div>
    );
}
