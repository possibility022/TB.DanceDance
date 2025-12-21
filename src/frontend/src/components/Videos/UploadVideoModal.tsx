import * as React from 'react';
import {Event} from '../../types/ApiModels/EventsAndGroups';
import {UploadVideoComponent} from './UploadVideoComponent';
import SharingWithType from '../../types/ApiModels/SharingWithType';

export interface IUploadVideoModalProps {
    sharingWith: SharingWithType.Event | SharingWithType.Private;
    event: Event | undefined
}

export function UploadVideoModal(props: IUploadVideoModalProps) {

    const [name, setName] = React.useState("")

    const [file, setFile] = React.useState<FileList>()

    const placeholder = props.sharingWith == SharingWithType.Private ? "Kink Swing - Comps Novice" : "Rama i Salta z Jordanem"
    const title = props.sharingWith == SharingWithType.Private ? "Wyślij jako prywatne nagranie" : "Wyślij nagranie do: " + props.event?.name

    return (
        <div className='box'>
            <h1 className="title">{title}</h1>
            <div className="field" hidden={file && file?.length > 1}>
                <label className="label">Nazwa</label>
                <div className="control">
                    <input className="input" type="text" value={name} onChange={(v) => setName(v.target.value)} placeholder={placeholder} />
                </div>
            </div>
            <UploadVideoComponent
                files={file}
                onFilesSelected={setFile}
                validateOnSending={() => {
                    if (props.sharingWith == SharingWithType.Private)
                        return true
                    else if (props.sharingWith == SharingWithType.Event) {
                        if (!props.event)
                            throw new Error("event not selected")

                        return true
                    } else {
                        throw new Error("Wrong upload type: " + props.sharingWith)
                    }
                }}
                getSendingDetails={() => {

                    let sharingWith = SharingWithType.NotSpecified
                    let assignedTo = undefined

                    if (props.sharingWith == SharingWithType.Private) {
                        sharingWith = SharingWithType.Private
                    } else if (props.sharingWith == SharingWithType.Event) {
                        sharingWith = SharingWithType.Event
                        assignedTo = props.event?.id
                    } else {
                        throw new Error("Wrong upload type: " + props.sharingWith)
                    }

                    return {
                        assignedTo: assignedTo,
                        onComplete: () => console.log("completed"),
                        sharingWithType: sharingWith,
                        videoName: name
                    }
                }}
            ></UploadVideoComponent>
        </div>
    );
}
