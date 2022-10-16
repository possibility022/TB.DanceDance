import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import LoginButton from '../components/LoginLogoutComponents/LoginButton';
import { AuthConsumer } from '../providers/AuthProvider';
import { IAuthService } from '../services/AuthService';
import { VideoInfoService } from "../services/VideoInfoService"
import VideoInformations from "../types/VideoInformations"


const Home = () => {

    const service = new VideoInfoService()
    const navigate = useNavigate();

    useEffect(() => {
        service.LoadVideos().then((v) => {
            setVideos(v)
        }).catch(r => {
            console.error(r)
        })
    }, []);

    const [videos, setVideos] = useState<Array<VideoInformations>>([]);

    return <AuthConsumer>
        {({ isAuthenticated, getAccessToken, signinRedirect }: IAuthService) => {
            return (

                <section className="section">
                    <div className="container">
                        {isAuthenticated() ?
                            <button className="button" onClick={() => { navigate('/videos') }}>Zatańczmy</button>
                            : <LoginButton signinRedirect={signinRedirect}></LoginButton>
                        }
                    </div>
                </section>

            );
        }}
    </AuthConsumer>
};

export default Home;
