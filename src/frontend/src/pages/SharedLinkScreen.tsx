import React, {useEffect, useState} from 'react';
import {useParams} from "react-router-dom";
import sharingService from "../services/SharingService";
import SharedVideoInfoResponse from "../types/ApiModels/Sharing/SharedVideoInfoResponse";
import VideoPlayerComponent from "../components/Videos/VideoPlayerComponent";
import CommentsList from "../components/Comments/CommentsList";
import {CommentsContext} from "../components/Comments/CommentsContext";
import {CommentResponse} from "../types/ApiModels/Comments/CommentResponse";

function SharedLinkScreen() {

    const params = useParams();
    const [videoInfo, setVideoInfo] = useState<SharedVideoInfoResponse>()
    const [url, setUrl] = useState<string>()
    const [error, setError] = useState<string>()

    const [comments, setComments] = useState<CommentResponse[]>([])
    const [commentsAvailable, setCommentsAvailable] = useState(false)
    const [commentsLoading, setCommentsLoading] = useState(false)

    const loadCommentsAsync = async () => {
        setCommentsLoading(true)
        try {
            // Mock data - replace with actual API call
            const mockComments: CommentResponse[] = [
                {
                    "id": "23474a48-a1e1-457c-b581-bd0e195a6725",
                    "videoId": "c4029eb1-23ad-417b-9a3b-1b2ad4751c0b",
                    "authorName": undefined,
                    "content": "This is an anonymous comment - no auth token!",
                    "createdAt": new Date(),
                    "updatedAt": null,
                    "isHidden": false,
                    "isAnonymous": true,
                    "isReported": false,
                    "reportedReason": null,
                    "isOwn": false,
                    "canModerate": true
                },
                {
                    "id": "96acc427-cf8e-4eb4-aaf8-09448a90b9ea",
                    "videoId": "c4029eb1-23ad-417b-9a3b-1b2ad4751c0b",
                    "authorName": "Tom B",
                    "content": "This is a test comment from authenticated user!",
                    "createdAt": new Date(),
                    "updatedAt": null,
                    "isAnonymous": false,
                    "isHidden": false,
                    "isReported": false,
                    "reportedReason": null,
                    "isOwn": true,
                    "canModerate": true
                },
                {
                    "id": "db8c7fff-a54d-437e-8ae8-29d3c155d595",
                    "videoId": "c4029eb1-23ad-417b-9a3b-1b2ad4751c0b",
                    "authorName": undefined,
                    "content": "This is an anonymous comment - no auth token!",
                    "createdAt": new Date(),
                    "updatedAt": null,
                    "isAnonymous": true,
                    "isHidden": false,
                    "isReported": false,
                    "reportedReason": null,
                    "isOwn": false,
                    "canModerate": true
                }
            ]
            setComments(mockComments)
        } finally {
            setCommentsLoading(false)
        }
    }

    const addCommentAsync = async (comment: string) => {
        // Implement add comment logic
    }

    const deleteCommentAsync = async (commentId: string) => {
        // Implement delete comment logic
    }

    useEffect(() => {
        const linkIdParam = params.linkId as string

        if (!linkIdParam) {
            setError('Nieprawidłowy link')
            return
        }

        sharingService.getVideoInformationFromLink(linkIdParam)
            .then(res => {
                setCommentsAvailable(true)
                setVideoInfo(res.data)
                setUrl(sharingService.getVideUrlByLinkId(linkIdParam))
            })
            .catch(error => {
                if (error.response?.status === 404) {
                    setError("Link nie istnieje lub wygasł :(. Poproś o nowy link")
                } else {
                    setError("Wystąpił błąd podczas pobierania linku")
                }
            })

    }, [])

    function renderComments() {
        if (!videoInfo)
            return

        return (
            <CommentsContext.Provider
                value={{
                    comments,
                    commentsAvailable,
                    commentsLoading,
                    addCommentAsync,
                    deleteCommentAsync,
                    loadCommentsAsync
                }}>
                <CommentsList/>
            </CommentsContext.Provider>
        )
    }

    function renderPage() {
        if (!url)
            return

        return <div>
            <VideoPlayerComponent videoId={undefined} sharedScope={undefined} videoList={[]} url={url}/>
        </div>
    }


    return (
        <div className="container">
            <h4 className="title is-5">{videoInfo?.name}</h4>
            {renderPage()}
            {renderComments()}

            {error && <p className="has-text-danger">{error}</p>}
        </div>
    );
}

export default SharedLinkScreen;