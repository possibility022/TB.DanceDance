import { useContext, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AccessToVideoRequestForm } from '../components/AccessToVideoReqest/AccessToVideoRequestForm';
import LoginButton from '../components/LoginLogoutComponents/LoginButton';
import { AuthContext } from '../providers/AuthProvider';
import { VideoInfoService } from "../services/VideoInfoService"
import VideoInformations from "../types/VideoInformations"


const Home = () => {

    const service = new VideoInfoService()
    const navigate = useNavigate();
    const authContext = useContext(AuthContext)

    useEffect(() => {
        service.LoadVideos().then((v) => {
            setVideos(v)
        }).catch(r => {
            console.error(r)
        })
    }, []);

    const envVariables = JSON.stringify(process.env)

    const [videos, setVideos] = useState<Array<VideoInformations>>([]);

    return (

        <section className="section">
            <div className="container">
                {authContext.isAuthenticated() ?
                    <button className="button" onClick={() => { navigate('/videos') }}>Zatańczmy</button>
                    : <LoginButton signinRedirect={() => authContext.signinRedirect()}></LoginButton>
                }
            </div>
            <div>
                <AccessToVideoRequestForm placeholder=''></AccessToVideoRequestForm>
            </div>
            <div hidden={true}>
                <p>{envVariables}</p>
            </div>
        </section>

    );
};

export default Home;
