import { faUpload } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import * as React from 'react';
import { useState } from 'react';
import ISharedVideoInformation from '../../types/ApiModels/SharedVideoInformation';
import SharingWithType from '../../types/ApiModels/SharingWithType';
import videoInfoService from '../../services/VideoInfoService';
import { Button } from '../Button';

export interface IUploadVideoComponentProps {
    files: FileList | undefined
    onFilesSelected?: (files: FileList) => void
    validateOnSending: () => boolean
    getSendingDetails: () => {
        videoName?: string
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
    const [sendCounter, setSendCounter] = useState(0)


    const getSentSuccessfullyMessage = () => {
        if (wasSentSuccessfully === true)
            return <p className="help is-success">Udało się</p>
        else if (wasSentSuccessfully === false)
            return <p className="help is-danger">Coś poszło nie tak :(</p>
    }

    const onFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (event.target.files && event.target.files.length > 0) {
            const selectedFile = event.target.files
            if (props.onFilesSelected)
                props.onFilesSelected(selectedFile)
        }
    }

    const validateFile = () => {
        let res: boolean
        if (props.files != undefined)
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
        setSendCounter(0)
        setWasTryingToSend(true)
        const isValid = props.validateOnSending()
        const fileIsValid = validateFile()

        if (!isValid || !fileIsValid)
        {
            return
        }

        const sendingDetails = props.getSendingDetails()
        uploadMany()
            .then(() => {
                setWasSentSuccessfully(true)
                sendingDetails.onComplete(true)
            })
            .catch(e => {
                console.error(e)
                sendingDetails.onComplete(false)
            })
    }

    const uploadMany = async () => {
        if (props.files) {

            for (let i = 0; i < props.files.length; i++) {
                const file = props.files[i]

                setBytestToTransfer(file.size)
                setBytesTransfered(0)

                const sendingDetails = props.getSendingDetails()

                let nameOfVideo = ''
                if (sendingDetails.videoName)
                    nameOfVideo = sendingDetails.videoName
                else
                    nameOfVideo = file.name

                const data: ISharedVideoInformation = {
                    nameOfVideo: nameOfVideo,
                    fileName: file.name,
                    recordedTimeUtc: new Date(file.lastModified),
                    sharedWith: sendingDetails.assignedTo,
                    sharingWithType: sendingDetails.sharingWithType
                }

                await videoInfoService.UploadVideo(data, file,
                    (e) => setBytesTransfered(e))

                setSendCounter(sendCounter + 1)
            }
        }
    }

    const renderFilesList = () => {

        const toReturn = new Array<JSX.Element>()

        if (!props.files)
            return null

        for (let i = 0; i < props.files.length; i++) {
            const file = props.files[i]
            toReturn.push(
            <li key={file.name}>
                {file.name}
            </li>)
        }

        return toReturn
    }

    const renderFileListHeader = () => {
        if (!props.files || props.files.length == 0)
            return null

        return <div>Wybrane pliki:</div>
    }

    return (
        <div>
            <div className="file has-name is-fullwidth">
                <label className="file-label">
                    <input className="file-input" type="file" name="resume" multiple={true} onChange={(e) => onFileChange(e)} />
                    <span className="file-cta">
                        <span className="file-icon">
                            <FontAwesomeIcon icon={faUpload} />
                            <i className="fas fa-upload"></i>
                        </span>
                        <span className="file-label">
                            Wybierz Pliki
                        </span>
                    </span>
                </label>
                {getFileVerificationMessage()}
            </div>
            <br />
            {renderFileListHeader()}
            <ul>
                {renderFilesList()}
            </ul>
            <br />

            <div className='container'>

                <div className="field">
                    <p className="control">
                        <Button onClick={upload}>
                            Wyślij nagranie
                        </Button>
                    </p>
                </div>
            </div>
            <br />

            <nav className="level">
                <div className="level-item has-text-centered">
                    <span className="tag is-medium">
                        {sendCounter}/{props.files?.length ?? 0}
                    </span>
                    <progress className="progress is-success" value={bytesTransfered} max={bytestToTransfer}></progress>

                </div>
            </nav>
            {getSentSuccessfullyMessage()}
        </div>
    );
}
