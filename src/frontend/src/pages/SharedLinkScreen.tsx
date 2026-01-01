import React, {useEffect, useState} from 'react';
import {useParams} from "react-router-dom";
import sharingService from "../services/SharingService";
import SharedVideoInfoResponse from "../types/ApiModels/Sharing/SharedVideoInfoResponse";
import VideoPlayerComponent from "../components/Videos/VideoPlayerComponent";

function SharedLinkScreen() {

    const params = useParams();
    const [videoInfo, setVideoInfo] = useState<SharedVideoInfoResponse>()
    const [url, setUrl] = useState<string>()
    const [error, setError] = useState<string>()


    useEffect(() => {
        const linkId = params.linkId as string

        if (!linkId)
        {
            setError('Nieprawidłowy link')
            return
        }

        sharingService.getVideoInformationFromLink(linkId)
            .then(res => {
                setVideoInfo(res.data)
                setUrl(sharingService.getVideUrlByLinkId(linkId))
            })
            .catch(error => {
                if (error.response?.status === 404)
                {
                    setError("Link nie istnieje lub wygasł :(. Poproś o nowy link")
                } else {
                    setError("Wystąpił błąd podczas pobierania linku")
                }
            })

    }, [])


    return (
        <div className="container">
            <h4 className="title is-5">{videoInfo?.name}</h4>
            {url && <VideoPlayerComponent videoId={undefined} sharedScope={undefined} videoList={[]} url={url}/>}
            {error && <p className="has-text-danger">{error}</p>}
        </div>
    );
}

export default SharedLinkScreen;