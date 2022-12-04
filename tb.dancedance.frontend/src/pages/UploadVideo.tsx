import * as React from 'react';

export interface IUploadVideoProps {
  some?: string
}

import videoInfoService from '../services/VideoInfoService'

export function UploadVideo (props: IUploadVideoProps) {

    const upload = () => {
        console.log("upload")
        videoInfoService.UploadVideo()
          .catch(e => {console.error(e)});
    }

  return (
    <div>
        <button onClick={upload}>
            Wyślij nagranie
        </button>
    </div>
  );
}
