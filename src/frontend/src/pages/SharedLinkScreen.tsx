import React, {useEffect, useState} from 'react';
import {useParams} from "react-router-dom";
import sharingService from "../services/SharingService";
import SharedVideoInfoResponse from "../types/ApiModels/Sharing/SharedVideoInfoResponse";
import VideoPlayerComponent from "../components/Videos/VideoPlayerComponent";
import CommentsComponent from "../components/Comments/CommentsComponent";
import {authService} from "../providers/AuthProvider";

function SharedLinkScreen() {

    const params = useParams();
    const [videoInfo, setVideoInfo] = useState<SharedVideoInfoResponse>()
    const [url, setUrl] = useState<string>()
    const [error, setError] = useState<string>()
    const [allowAdding, setAllowAdding] = useState(false)

    useEffect(() => {
        const linkIdParam = params.linkId as string

        if (!linkIdParam) {
            setError('Nieprawidłowy link')
            return
        }

        sharingService.getVideoInformationFromLink(linkIdParam)
            .then(res => {
                setVideoInfo(res.data)
                setUrl(sharingService.getVideUrlByLinkId(linkIdParam))

                if (!res.data.allowCommentsOnThisLink)
                    return

                if (authService.isAuthenticated()) {
                    setAllowAdding(res.data.allowCommentsOnThisLink)
                } else {
                    setAllowAdding(res.data.allowAnonymousCommentsOnThisLink)
                }
            })
            .catch(error => {
                if (error.response?.status === 404) {
                    setError("Link nie istnieje lub wygasł :(. Poproś o nowy link")
                } else {
                    setError("Wystąpił błąd podczas pobierania linku")
                }
            })

    }, [])

    function renderPage() {
        if (!url)
            return

        if (!videoInfo)
            return

        return <div>
            <VideoPlayerComponent videoId={undefined} sharedScope={undefined} videoList={[]} url={url}/>
            <CommentsComponent videoId={videoInfo!.videoId} allowAdding={allowAdding} linkId={params.linkId as string}/>
        </div>
    }


    return (
        <div className="container">
            <h4 className="title is-5">{videoInfo?.name}</h4>
            {renderPage()}
            {error && <p className="has-text-danger">{error}</p>}
        </div>
    );
}

export default SharedLinkScreen;