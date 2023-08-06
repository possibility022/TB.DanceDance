import * as React from 'react';
import { Event } from '../../types/ApiModels/EventsAndGroups';
import { UploadVideoComponent } from './UploadVideoComponent';
import SharingWithType from '../../types/ApiModels/SharingWithType';

export interface IUploadVideoModalProps {
    event: Event | undefined
}

export function UploadVideoModal(props: IUploadVideoModalProps) {

    const [name, setName] = React.useState("")

    const [file, setFile] = React.useState<FileList>()

    return (
        <div className='container has-background-white p-6'>
            <h1 className="title">Wyślij nagranie do: {props.event?.name}</h1>
            <div className="field">
                <label className="label">Nazwa</label>
                <div className="control">
                    <input className="input" type="text" value={name} onChange={(v) => setName(v.target.value)} placeholder="Rama i Salta z Jordanem" />
                </div>
            </div>
            <UploadVideoComponent
                files={file}
                onFilesSelected={setFile}
                validateOnSending={() => props.event != undefined}
                getSendingDetails={() => {

                    if (!props.event)
                        throw new Error("event not selected")

                    return {
                        assignedTo: props.event.id,
                        onComplete: () => console.log("completed"),
                        sharingWithType: SharingWithType.Event,
                        videoName: name
                    }
                }}
            ></UploadVideoComponent>
        </div>
    );
}
