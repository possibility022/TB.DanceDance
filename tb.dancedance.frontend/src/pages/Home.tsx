import React, { useEffect, useState } from 'react';
import { VideoList } from "../components/Videos/VideoList"
import { VideoInfoService } from "../services/VideoInfoService"
import VideoInformations from "../types/VideoInformations"


const Home = () => {

    const service = new VideoInfoService()

    useEffect(() => {
        service.LoadVideos().then((v) => {
            setVideos(v)
        }).catch(r => {
            console.error(r)
        })
    }, []);

    const [videos, setVideos] = useState<Array<VideoInformations>>([]);

    return (
        <section className="section">
            <div className="container">
                <p className="subtitle">
                    Zatańczmy!
                </p>

                <VideoList Videos={videos}></VideoList>

            </div>
        </section>
    );
};

export default Home;
