import { useEffect, useState, useContext } from 'react';
import ReactPlayer from 'react-player';
import { AuthContext } from '../providers/AuthProvider';

interface IVideoPlayProps {
  videoUrl: string
}

export function PrivateScreen(props: IVideoPlayProps) {

  const authContext = useContext(AuthContext)

  const [url, setUrl] = useState<string | undefined>(undefined)

  useEffect(() => {
    authContext.getAccessToken()
      .then((token) => {
        if (token)
          // todo, improve authorization way
          setUrl(`${props.videoUrl}?token=${token}`)
        setUrl(props.videoUrl)

      })
      .catch(e => console.log(e))

      return () => {
        // todo cleanup / aboard
      }
  }, [])


  return (
    <ReactPlayer controls={true} url={url}
    ></ReactPlayer>
  )
}
