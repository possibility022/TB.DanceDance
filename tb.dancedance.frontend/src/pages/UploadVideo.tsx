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

  const [file, setFile] = useState<FileList>()
  const [availableGroups, setAvailableGroups] = useState<Array<IToAssign>>([])
  const [selectedGroupIndex, setSelectedGroupIndex] = useState(-1)

  const [isLoading, setIsLoading] = useState(false);

  const [videoName] = useState<string>('')
  const [groupSelectionIsValid, setGroupSelectionIsValid] = useState(false)
  const [wasTryingToSend, setWasTryingToSend] = useState(false)

  const availableGroupNames = availableGroups.map(r => r.name)

  React.useEffect(() => {
    setIsLoading(true)
    videoInfoService.GetUserEventsAndGroups()
      .then(v => {

        const availableGroups = new Array<IToAssign>()

        for (const el of v.assigned.groups)
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

  const validateGroupSelection = () => {
    const isValid = availableGroups.length > 0 && selectedGroupIndex >= 0
    setGroupSelectionIsValid(isValid)
    return isValid
  }

  const validateInput = (): boolean => {
    const groupIsValid = validateGroupSelection()

    setWasTryingToSend(true)
    return groupIsValid
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
        files={file}
        onFilesSelected={setFile}
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
