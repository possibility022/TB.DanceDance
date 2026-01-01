import React, {useEffect, useRef, useState} from 'react';
import {VideoList} from "../components/Videos/VideoList";
import videoInfoService from "../services/VideoInfoService";
import VideoInformation from "../types/ApiModels/VideoInformation";
import {Button} from "../components/Button";
import {UploadVideoModal} from "../components/Videos/UploadVideoModal";
import SharingWithType from "../types/ApiModels/SharingWithType";
import ShareVideoModal from "../components/Sharing/ShareVideoModal";


function PrivateVideos() {

    const [videos, setVideos] = useState<VideoInformation[]>([])
    const [showShareModal, setShowShareModal] = useState<{
        videoInformation?: VideoInformation,
        show: boolean
    }>({videoInformation: undefined, show: false})

    const uploadModalRef = useRef<HTMLDivElement>(null)
    const closeUploadModal = () => {
        uploadModalRef.current?.classList.remove('is-active')
    }

    useEffect(() => {
        videoInfoService.GetPrivateVideos().then(r => {
            setVideos(r)
        })
    }, [])

    const openModal = () => {
        uploadModalRef.current?.classList.add('is-active')
    }

    const renderModal = () => {
        if (showShareModal.show)
            return <ShareVideoModal videoInfo={showShareModal.videoInformation!}
                                    onCloseClick={() => setShowShareModal({
                                        videoInformation: undefined,
                                        show: false
                                    })}></ShareVideoModal>
        else
            return <></>
    }

    return (
        <div className='container is-max-desktop'>
            <nav className="level">
                <div className="level-left">
                    <div className="level-item">
                        <p className="subtitle is-5">
                            <Button onClick={openModal}>
                                Dodaj prywatne nagranie
                            </Button>
                        </p>
                    </div>
                </div>
            </nav>

            {renderModal()}

            <div className="content">
                <h1>Prywatna biblioteka</h1>
                <p>
                    To jest miejsce na Twoje osobiste nagrania, do których dostęp będziesz mieć tylko Ty.
                    Jest to dobre miejsce na przechowanie filmów z np. z Twojego udziału w konkursach lub własnych
                    treningów.
                </p>

                <h2>Lista prywatnych nagrań</h2>

                <VideoList videos={videos} enableShare={true}
                           onShareClick={(videoInformation) => setShowShareModal({
                               videoInformation: videoInformation,
                               show: true
                           })}></VideoList>
            </div>

            <div className="modal" ref={uploadModalRef}>
                <div className="modal-background" onClick={closeUploadModal}></div>
                <div className="modal-content">
                    <UploadVideoModal
                        sharingWith={SharingWithType.Private}
                        event={undefined}
                    ></UploadVideoModal>
                </div>
                <button className="modal-close is-large" aria-label="close" onClick={closeUploadModal}></button>
            </div>

        </div>
    );
}

export default PrivateVideos;