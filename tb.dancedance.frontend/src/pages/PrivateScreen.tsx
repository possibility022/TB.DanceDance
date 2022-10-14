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

  const constructUrl = (originalUrl: string, token: string | null) => {
    if (token)
      // todo, improve authorization way
      return `${originalUrl}?token=${token}`
    return originalUrl
  }

  return <AuthConsumer>
    {
      ({ getAccessToken }: IAuthService) => {
        return (
          <ReactPlayer controls={true} url={constructUrl(props.videoUrl, getAccessToken())}
          ></ReactPlayer>
        )
      }
    }
  </AuthConsumer >
}
