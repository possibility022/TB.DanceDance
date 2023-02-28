import { useContext } from 'react';
import { useNavigate } from 'react-router-dom';
import { AccessToVideoRequestForm } from '../components/AccessToVideoReqest/AccessToVideoRequestForm';
import LoginButton from '../components/LoginLogoutComponents/LoginButton';
import { AuthContext } from '../providers/AuthProvider';


const Home = () => {

    const navigate = useNavigate();
    const authContext = useContext(AuthContext)

    const envVariables = JSON.stringify(process.env)

    return (

        <section className="section">
            <div className="container">
                {authContext.isAuthenticated() ?
                    <button className="button" onClick={() => { navigate('/videos') }}>Zatańczmy</button>
                    : <LoginButton signinRedirect={() => authContext.signinRedirect()}></LoginButton>
                }
            </div>
            <div hidden={true}>
                <p>{envVariables}</p>
            </div>
        </section>

    );
};

export default Home;
