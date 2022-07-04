import React, { useEffect, useState } from 'react';
import { VideoList } from "../components/Videos/VideoList"
import { VideoInfoService } from "../services/VideoInfoService"
import VideoInformations from "../types/VideoInformations"


const Home = () => {

    const service = new VideoInfoService()

    useEffect(() => {
        // eslint-disable-next-line @typescript-eslint/no-floating-promises
        service.LoadVideos().then((v) => {
            setVideos(v)
        }).catch(r => {
            console.log(r)
        })
    }, []);

    const [videos, setVideos] = useState<Array<VideoInformations>>([]);

    return (
        <section className="section">
            <div className="container">
                <p className="subtitle">
                    Zatańczmy <strong>Razem</strong>!
                </p>

                <VideoList Videos={videos}></VideoList>

            </div>
        </section>
    );
};

export default Home;
