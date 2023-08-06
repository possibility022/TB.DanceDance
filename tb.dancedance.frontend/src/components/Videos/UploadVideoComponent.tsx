import { faUpload } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import * as React from 'react';
import { useState } from 'react';
import ISharedVideoInformation from '../../types/ApiModels/SharedVideoInformation';
import SharingWithType from '../../types/ApiModels/SharingWithType';
import videoInfoService from '../../services/VideoInfoService';
import { Button } from '../Button';

export interface IUploadVideoComponentProps {
    file: File | undefined
    onFileSelected?: (file: File) => void
    validateOnSending: () => boolean
    getSendingDetails: () => {
        videoName: string
        assignedTo: string
        sharingWithType: SharingWithType
        onComplete: (success: boolean) => void
    }
}

export function UploadVideoComponent(props: IUploadVideoComponentProps) {

    const [bytesTransfered, setBytesTransfered] = useState(0)
    const [bytestToTransfer, setBytestToTransfer] = useState(0)
    const [fileSelectionIsValid, setFileSelectionIsValid] = useState(false)
    const [wasSentSuccessfully, setWasSentSuccessfully] = useState<boolean | null>(null)
    const [wasTryingToSend, setWasTryingToSend] = useState(false)


    const getSentSuccessfullyMessage = () => {
        if (wasSentSuccessfully === true)
            return <p className="help is-success">Udało się</p>
        else if (wasSentSuccessfully === false)
            return <p className="help is-danger">Coś poszło nie tak :(</p>
    }

    const onFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (event.target.files && event.target.files.length == 1) {
            const selectedFile = event.target.files[0]
            if (props.onFileSelected)
                props.onFileSelected(selectedFile)
        }
    }

    const validateFile = () => {
        let res: boolean
        if (props.file != undefined)
            res = true
        else
            res = false

        setFileSelectionIsValid(res)
        return res
    }

    const getFileVerificationMessage = () => {
        if (!wasTryingToSend)
            return null
        if (!fileSelectionIsValid)
            return <p className="help is-danger">Musisz wybrać poprawny plik.</p>
    }

    const upload = () => {
        setWasTryingToSend(true)
        props.validateOnSending()
        validateFile()

        if (props.file) {
            setBytestToTransfer(props.file.size)
            setBytesTransfered(0)

            const sendingDetails = props.getSendingDetails()

            const data: ISharedVideoInformation = {
                nameOfVideo: sendingDetails.videoName,
                fileName: props.file.name,
                recordedTimeUtc: new Date(props.file.lastModified),
                sharedWith: sendingDetails.assignedTo,
                sharingWithType: sendingDetails.sharingWithType
            }

            videoInfoService.UploadVideo(data, props.file,
                (e) => setBytesTransfered(e))
                .then(() => {
                    setWasSentSuccessfully(true)
                    sendingDetails.onComplete(true)
                })
                .catch(e => {
                    console.error(e)
                    sendingDetails.onComplete(false)
                })
        }
    }

    return (
        <div>
            <div className="file has-name is-fullwidth">
                <label className="file-label">
                    <input className="file-input" type="file" name="resume" onChange={(e) => onFileChange(e)} />
                    <span className="file-cta">
                        <span className="file-icon">
                            <FontAwesomeIcon icon={faUpload} />
                            <i className="fas fa-upload"></i>
                        </span>
                        <span className="file-label">
                            Wybierz Plik
                        </span>
                    </span>
                    <span className="file-name">
                        {props.file?.name}
                    </span>
                </label>
                {getFileVerificationMessage()}
            </div>

            <div className="field">
                <p className="control">
                    <Button onClick={upload}>
                        Wyślij nagranie
                    </Button>
                </p>
            </div>

            <progress className="progress is-success" value={bytesTransfered} max={bytestToTransfer}></progress>
            {getSentSuccessfullyMessage()}
        </div>
    );
}
