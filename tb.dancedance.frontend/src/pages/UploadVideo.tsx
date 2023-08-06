import * as React from 'react';
import { useState } from 'react';
import { Dropdown } from '../components/Dropdown';


import videoInfoService from '../services/VideoInfoService'
import SharingWithType from "../types/ApiModels/SharingWithType";
import { UploadVideoComponent } from '../components/Videos/UploadVideoComponent';

interface IToAssign {
  id: string
  isEvent: boolean
  name: string
}

export function UploadVideo() {

  const [file, setFile] = useState<File>()
  const [availableGroups, setAvailableGroups] = useState<Array<IToAssign>>([])
  const [selectedGroupIndex, setSelectedGroupIndex] = useState(-1)

  const [isLoading, setIsLoading] = useState(false);

  const [videoName, setVideoName] = useState<string>('')
  const [videoNameIsValid, setVideoNameIsValid] = useState(false)
  const [groupSelectionIsValid, setGroupSelectionIsValid] = useState(false)
  const [wasTryingToSend, setWasTryingToSend] = useState(false)



  const availableGroupNames = availableGroups.map(r => r.name)



  React.useEffect(() => {
    setIsLoading(true)
    videoInfoService.GetUserEventsAndGroups()
      .then(v => {

        const availableGroups = new Array<IToAssign>()

        for (const el of v.events)
          availableGroups.push({
            id: el.id,
            isEvent: true,
            name: el.name
          })

        for (const el of v.groups)
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
    let isValid = false
    if (videoName && videoName.length > 5 && videoName.length < 100) {
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

  const validateInput = (): boolean => {
    const groupIsValid = validateGroupSelection()
    const videoNameIsValid = validateVideoName()

    setWasTryingToSend(true)
    return groupIsValid && videoNameIsValid
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

      <UploadVideoComponent
        file={file}
        onFileSelected={setFile}
        validateOnSending={() => validateInput()}
        getSendingDetails={() => {
          return {
            videoName: videoName,
            assignedTo: availableGroups[selectedGroupIndex].id,
            sharingWithType: availableGroups[selectedGroupIndex].isEvent ? SharingWithType.Event : SharingWithType.Group,
            onComplete: () => { console.log('Sending complete') }
          }
        }}
      ></UploadVideoComponent>

    </div>
  );
}
