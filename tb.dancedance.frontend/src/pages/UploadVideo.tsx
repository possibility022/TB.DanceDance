import { faUpload } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import * as React from 'react';
import { useState } from 'react';
import { Button } from '../components/Button';
import { Dropdown } from '../components/Dropdown';


import videoInfoService from '../services/VideoInfoService'
import ISharedVideoInformation from '../types/ApiModels/SharedVideoInformation';
import SharingWithType from "../types/ApiModels/SharingWithType";

interface IToAssign {
  id: string
  isEvent: boolean
  name: string
}

export function UploadVideo() {

  const [file, setFile] = useState<File>()
  const [availableGroups, setAvailableGroups] = useState<Array<IToAssign>>([]);
  const [selectedGroupIndex, setSelectedGroupIndex] = useState(-1);

  const [isLoading, setIsLoading] = useState(false);
  const [wasSentSuccessfully, setWasSentSuccessfully] = useState<boolean | null>(null);

  const [videoName, setVideoName] = useState<string>('')
  const [videoNameIsValid, setVideoNameIsValid] = useState(false);
  const [groupSelectionIsValid, setGroupSelectionIsValid] = useState(false);
  const [fileSelectionIsValid, setFileSelectionIsValid] = useState(false);

  const [bytesTransfered, setBytesTransfered] = useState(0);
  const [bytestToTransfer, setBytestToTransfer] = useState(0);

  const [wasTryingToSend, setWasTryingToSend] = useState(false);

  const availableGroupNames = availableGroups.map(r => r.name)

  const onFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.files && event.target.files.length == 1) {
      const selectedFile = event.target.files[0]
      setFile(selectedFile)
    }
  }

  React.useEffect(() => {
    setIsLoading(true)
    videoInfoService.GetUserAccess()
      .then(v => {

        const availableGroups = new Array<IToAssign>()

        for(const el of v.events)
          availableGroups.push({
            id: el.id,
            isEvent: true,
            name: el.name
          })

        for(const el of v.groups)
          availableGroups.push({
            id: el.id,
            isEvent: false,
            name: el.name
          })

        setAvailableGroups(availableGroups)
        setIsLoading(false)
      })
      .catch(e => console.log(e))

    return () => {
      // todo cleanup = abort request
    }
  }, []);

  const validateVideoName = () => {
    const regex = new RegExp('^[-^:) _a-zA-Z0-9]*$')
    let isValid = false
    if (videoName && videoName.length > 5 && videoName.length < 100 && regex.test(videoName)) {
      isValid = true
    }
    else {
      isValid = false
    }
    setVideoNameIsValid(isValid)
    return isValid
  }

  const validateGroupSelection = () => {
    const isValid = availableGroups.length > 0 && selectedGroupIndex >= 0
    setGroupSelectionIsValid(isValid)
    return isValid
  }

  const validateFile = () => {
    let res: boolean
    if (file != undefined)
      res = true
    else
      res = false

    setFileSelectionIsValid(res)
    return res
  }

  const validateInput = (): boolean => {
    const groupIsValid = validateGroupSelection()
    const videoNameIsValid = validateVideoName()
    const fileIsValid = validateFile()

    setWasTryingToSend(true)
    return groupIsValid && videoNameIsValid && fileIsValid
  }



  const upload = () => {

    const inputIsValid = validateInput()

    if (file && inputIsValid) {
      setBytestToTransfer(file.size)
      setBytesTransfered(0)

      const selected = availableGroups[selectedGroupIndex]

      const data: ISharedVideoInformation = { 
        nameOfVideo: videoName,
        fileName: file.name,
        recordedTimeUtc: new Date(file.lastModified),
        sharedWith: selected.id,
        sharingWithType: selected.isEvent ? SharingWithType.Event : SharingWithType.Group
      }

      videoInfoService.UploadVideo(data, file,
        (e) => setBytesTransfered(e))
        .then(() => {
          setWasSentSuccessfully(true)
        })
        .catch(e => {
          console.error(e)
          setWasSentSuccessfully(false)
        })

    }
  }

  const getSentSuccessfullyMessage = () => {
    if (wasSentSuccessfully === true)
      return <p className="help is-success">Udało się</p>
    else if (wasSentSuccessfully === false)
      return <p className="help is-danger">Coś poszło nie tak :(</p>
  }

  const getNameVeryficationMessage = () => {
    if (!wasTryingToSend)
      return null

    if (videoNameIsValid)
      return null
    else
      return <p className="help is-danger">Coś jest nie tak. Nie wszystkie znaki specjalne są dozwolone :(. Nazwa musi zawierać conajmniej 5 znaków i nie więcej niż 100. </p>
  }

  const getGroupVeryficationMessage = () => {
    if (!wasTryingToSend)
      return null
    if (!groupSelectionIsValid)
      return <p className="help is-danger">Musisz wybrać grupę</p>
  }

  const getFileVerificationMessage = () => {
    if (!wasTryingToSend)
      return null
    if (!fileSelectionIsValid)
      return <p className="help is-danger">Musisz wybrać poprawny plik.</p>
  }


  return (
    <div>

      <div className="field">
        <label className="label">Nazwa Nagrania</label>
        <div className="control">
          <input className="input" type="text" placeholder="Korki podstawowe"
            value={videoName}
            onChange={(e) => setVideoName(e.target.value)} />
        </div>
        {getNameVeryficationMessage()}
      </div>

      <br />

      <div className="field">
        <label className="label">Jakiej grupie udostępnić?</label>

        <Dropdown
          items={availableGroupNames}
          selectedItemIndex={0}
          startWithUnselected={true}
          unselectedText='Do jakiej grupy chcesz dodać filmik?'
          isLoading={isLoading}
          onSelected={(v, i) => setSelectedGroupIndex(i)}
        />
        {getGroupVeryficationMessage()}
      </div>
      <br></br>

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
            {file?.name}
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
