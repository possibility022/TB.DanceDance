import * as React from 'react';
import { useState } from 'react';

export interface IUploadVideoProps {
  some?: string
}

import videoInfoService from '../services/VideoInfoService'

export function UploadVideo(props: IUploadVideoProps) {


  const [file, setFile] = useState<File>()

  const onFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.files && event.target.files.length == 1) {
      const selectedFile = event.target.files[0]
      setFile(selectedFile)
    }
  }

  const upload = () => {
    if (file)
      videoInfoService.UploadVideo(file)
        .catch(e => {
          console.error(e)
        })
  }

  return (
    <div>
      <label htmlFor="inputFile">file
        <input id="inputFile" type="file" onChange={(e) => onFileChange(e)} />
      </label>
      <button onClick={upload}>
        Wyślij nagranie
      </button>
    </div>
  );
}
