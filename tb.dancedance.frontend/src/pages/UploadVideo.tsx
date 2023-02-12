import { faUpload } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import * as React from 'react';
import { useState } from 'react';
import { Button } from '../components/Button';
import { Dropdown } from '../components/Dropdown';

export interface IUploadVideoProps {
  some?: string
}

import videoInfoService from '../services/VideoInfoService'
import ISharingScopeModel from '../types/SharingScopeModel';

export function UploadVideo(props: IUploadVideoProps) {

  const [file, setFile] = useState<File>()
  const [availableGroups, setAvailableGroups] = useState<Array<ISharingScopeModel>>([]);
  const [selectedGroupIndex, setSelectedGroupIndex] = useState(-1);

  const [isLoading, setIsLoading] = useState(false);

  const [groupSelectionIsValid, setGroupSelectionIsValid] = useState(false);


  const [videoName, setVideoName] = useState<string>()
  const [videoNameIsValid, setVideoNameIsValid] = useState(false);

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
    videoInfoService.GetAvailableGroups()
      .then(v => {
        setAvailableGroups(v)
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
    if (videoName && regex.test(videoName)) {
      isValid = true
    }
    else {
      isValid = false
    }
    setVideoNameIsValid(isValid)
  }

  const validateGroupSelection = () => {
    const isValid =  availableGroups.length > 0 && selectedGroupIndex >= 0
    setGroupSelectionIsValid(isValid)
  }

  const validateInput = (): boolean => {
    validateGroupSelection()
    validateVideoName()

    setWasTryingToSend(true)
    return videoNameIsValid && groupSelectionIsValid
  }

  const upload = () => {

    const inputIsValid = validateInput()

    if (file && inputIsValid)
      videoInfoService.UploadVideo(file)
        .catch(e => {
          console.error(e)
        })
  }

  const getNameVeryficationMessage = () => {
    if (!wasTryingToSend)
      return null

    if (videoNameIsValid)
      return <p className="help is-success" > Jest git</p>
    else
      return <p className="help is-danger">Coś jest nie tak. Nie wszystkie znaki specjalne są dozwolone :( </p>
  }

  const getGroupVeryficationMessage = () => {
    if (!wasTryingToSend)
      return null
    if (!groupSelectionIsValid)
      return <p className="help is-danger">Musisz wybrać grupę</p>
  }

  const message = () => {
    if (file?.name) {
      <div>{file.name}</div>
    } else {
      <div>Wybierz plik</div>
    }
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
        />
        {getGroupVeryficationMessage()}
      </div>

      {message()}
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
      </div>

      <div className="field">
        <p className="control">
          <Button onClick={upload}>
            Wyślij nagranie
          </Button>
        </p>
      </div>
    </div>
  );
}
