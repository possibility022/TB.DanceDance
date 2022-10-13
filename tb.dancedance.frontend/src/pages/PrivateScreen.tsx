import { Blob } from 'buffer';
import * as React from 'react';
import { useEffect, useState } from 'react';
import ReactPlayer from 'react-player';
import { AuthConsumer } from '../providers/AuthProvider';
import { AuthService, IAuthService, TokenProvider } from '../services/AuthService';
import { VideoInfoService } from '../services/VideoInfoService';

interface IVideoPlayProps {
  videoUrl: string
}

export function PrivateScreen(props: IVideoPlayProps) {


  const fetchVideo = async () => {
    const response = await fetch(props.videoUrl, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    })
    const blob = await response.blob()
    const url = URL.createObjectURL(blob as Blob)
    setUrl(url)
    //const url = URL.createObjectURL(blob)
    //setUrl(url)
  }

  const [url, setUrl] = useState<string>()
  const [token, setToken] = useState<string>('')

  const tokenProvider: TokenProvider = new AuthService()

  useEffect(() => {
    const t = tokenProvider.getAccessToken()
    if (t != token) {
      setToken(token)
      fetchVideo()
        .catch(e => console.error(e))
    }
  },[url])

  return <AuthConsumer>
    {
      ({ getAccessToken }: IAuthService) => {
        return (
          <ReactPlayer
            url={url}
            controls={true}
          // config={{
          //   file: {
          //     forceHLS: true,
          //     hlsOptions: {
          //       xhrSetup: function (xhr: XMLHttpRequest, url: unknown) {
          //         console.log(xhr, url)
          //         const token = getAccessToken()
          //         if (token)
          //           xhr.setRequestHeader('Authorization', `Bearer ${token}`)
          //         else
          //           console.error("token not provided")
          //       }
          //     }
          //   }
          // }
          // }
          >

          </ReactPlayer>

        )
      }
    }
  </AuthConsumer >
}
