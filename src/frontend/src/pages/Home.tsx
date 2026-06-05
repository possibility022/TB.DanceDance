import React, {useContext, useEffect, useState} from 'react';
import { useNavigate } from 'react-router-dom';
import LoginButton from '../components/LoginLogoutComponents/LoginButton';
import { AuthContext } from '../providers/AuthProvider';
import videoInfoService from '../services/VideoInfoService';
import { VideoInformation, VideoFromGroupInformation, EventModel2 as Event } from '../types/ApiModels/dancedance/apiModels';
import { Button } from '../components/Button';
import { formatDateToPlDate } from '../extensions/DateExtensions';


const Home = () => {

    const navigate = useNavigate();
    const authContext = useContext(AuthContext)

    const [groupVideos, setGroupVideos] = useState<VideoFromGroupInformation[]>([])
    const [events, setEvents] = useState<Event[]>([])
    const [privateVideos, setPrivateVideos] = useState<VideoInformation[]>([])
    const [isLoading, setIsLoading] = useState(false)
    const [recentVideos, setRecentVideos] = useState<VideoInformation[]>([])

    const envVariables = JSON.stringify(process.env)

    useEffect(() => {
        if (authContext.isAuthenticated()) {
            setIsLoading(true)

            Promise.all([
                videoInfoService.GetVideosFromGroups(),
                videoInfoService.GetUserEventsAndGroups(),
                videoInfoService.GetPrivateVideos()
            ])
            .then(([groups, eventsData, privateVids]) => {
                setGroupVideos(groups)
                setEvents(eventsData.assigned?.events ?? [])
                setPrivateVideos(privateVids)

                const allVideos: VideoInformation[] = [...groups, ...privateVids]

                const sorted = allVideos
                    .sort((a, b) => new Date(b.recordedDateTime!).getTime() - new Date(a.recordedDateTime!).getTime())
                    .slice(0, 5)
                setRecentVideos(sorted)
            })
            .catch(e => console.error(e))
            .finally(() => setIsLoading(false))
        }
    }, [authContext])

    const getLatestVideoDate = (videos: VideoInformation[]) => {
        if (videos.length === 0) return null
        const latest = videos.reduce((latest, video) =>
            new Date(video.recordedDateTime!) > new Date(latest.recordedDateTime!) ? video : latest
        )
        return latest.recordedDateTime
    }

    const renderAuthenticatedDashboard = () => {
        if (isLoading) {
            return (
                <div className="container">
                    <progress className="progress is-large is-info" max="100">Loading...</progress>
                </div>
            )
        }

        return (
            <div className="container">
                <section className="hero is-medium is-info is-bold">
                    <div className="hero-body">
                        <h1 className="title is-1">Dance Dance</h1>
                        <p className="subtitle is-4">Twoja biblioteka nagrań tanecznych</p>
                    </div>
                </section>

                <section className="section">
                    <div className="columns is-multiline">
                        {/* Group Videos Card */}
                        <div className="column is-4">
                            <div className="card" style={{ height: '100%', cursor: 'pointer' }} onClick={() => navigate('/videos')}>
                                <div className="card-content">
                                    <div className="content">
                                        <p className="title is-4">📹 Nagrania z Zajęć</p>
                                        <p className="subtitle is-5">
                                            {groupVideos.length} nagrań
                                        </p>
                                        {getLatestVideoDate(groupVideos) && (
                                            <p className="is-size-7">
                                                Ostatnie: {formatDateToPlDate(getLatestVideoDate(groupVideos)!)}
                                            </p>
                                        )}
                                        <p className="mt-3">
                                            Nagrania z twoich zajęć grupowych
                                        </p>
                                    </div>
                                </div>
                                <footer className="card-footer">
                                    <a className="card-footer-item">Zobacz więcej →</a>
                                </footer>
                            </div>
                        </div>

                        {/* Events Card */}
                        <div className="column is-4">
                            <div className="card" style={{ height: '100%', cursor: 'pointer' }} onClick={() => navigate('/events')}>
                                <div className="card-content">
                                    <div className="content">
                                        <p className="title is-4">🎉 Wydarzenia</p>
                                        <p className="subtitle is-5">
                                            {events.length} wydarzeń
                                        </p>
                                        {events.length > 0 && (
                                            <p className="is-size-7">
                                                Ostatnie: {events[0].name}
                                            </p>
                                        )}
                                        <p className="mt-3">
                                            Nagrania z eventów tanecznych
                                        </p>
                                    </div>
                                </div>
                                <footer className="card-footer">
                                    <a className="card-footer-item">Zobacz więcej →</a>
                                </footer>
                            </div>
                        </div>

                        {/* Private Videos Card */}
                        <div className="column is-4">
                            <div className="card" style={{ height: '100%', cursor: 'pointer' }} onClick={() => navigate('/videos/my')}>
                                <div className="card-content">
                                    <div className="content">
                                        <p className="title is-4">🔒 Prywatne Nagrania</p>
                                        <p className="subtitle is-5">
                                            {privateVideos.length} nagrań
                                        </p>
                                        {getLatestVideoDate(privateVideos) && (
                                            <p className="is-size-7">
                                                Ostatnie: {formatDateToPlDate(getLatestVideoDate(privateVideos)!)}
                                            </p>
                                        )}
                                        {!getLatestVideoDate(privateVideos) && (
                                            <p className="is-size-7">Brak prywatnych nagran</p>
                                        )}
                                        <p className="mt-3">
                                            Twoje konkursy i prywatne treningi
                                        </p>
                                    </div>
                                </div>
                                <footer className="card-footer">
                                    <a className="card-footer-item">Zobacz więcej →</a>
                                </footer>
                            </div>
                        </div>
                    </div>

                    {/* Quick Actions */}
                    <div className="box mt-5">
                        <h2 className="title is-4">Szybkie akcje</h2>
                        <div className="buttons">
                            <Button onClick={() => navigate('/videos/upload')}>
                                📤 Wyślij Nagranie
                            </Button>
                            <Button onClick={() => navigate('/videos/requestassignment')}>
                                🔓 Uzyskaj Dostęp
                            </Button>
                        </div>
                    </div>

                    {/* Recent Activity */}
                    {recentVideos.length > 0 && (
                        <div className="box mt-5">
                            <h2 className="title is-4">Ostatnia aktywność</h2>
                            <div className="content">
                                <table className="table is-fullwidth is-striped is-hoverable">
                                    <thead>
                                        <tr>
                                            <th>Nagranie</th>
                                            <th>Data</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {recentVideos.map(video => (
                                            <tr
                                                key={video.videoId}
                                                onClick={() => navigate(`/videos/${video.blobId}`)}
                                                style={{ cursor: 'pointer' }}
                                            >
                                                <td>{video.name}</td>
                                                <td>{formatDateToPlDate(video.recordedDateTime)}</td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    )}
                </section>

                <div hidden={true}>
                    <p>{envVariables}</p>
                </div>
            </div>
        )
    }

    const renderLandingPage = () => {
        return (
            <div className="container">
                <section className="hero is-large is-primary is-bold">
                    <div className="hero-body">
                        <h1 className="title is-1">Dance Dance</h1>
                        <p className="subtitle is-3">Organizuj i udostępniaj nagrania taneczne</p>
                    </div>
                </section>

                <section className="section">
                    <div className="columns is-multiline">
                        <div className="column is-4">
                            <div className="box has-text-centered" style={{ height: '100%' }}>
                                <span className="is-size-1">📹</span>
                                <h2 className="title is-4 mt-3">Zajęcia grupowe</h2>
                                <p>Dostęp do nagrań z twoich zajęć tanecznych. Przeglądaj materiały z kolejnych sesji treningowych.</p>
                            </div>
                        </div>

                        <div className="column is-4">
                            <div className="box has-text-centered" style={{ height: '100%' }}>
                                <span className="is-size-1">🎉</span>
                                <h2 className="title is-4 mt-3">Wydarzenia</h2>
                                <p>Nagrania z eventów tanecznych, warsztatów i pokazów. Wspomnienia z ważnych wydarzeń.</p>
                            </div>
                        </div>

                        <div className="column is-4">
                            <div className="box has-text-centered" style={{ height: '100%' }}>
                                <span className="is-size-1">🔒</span>
                                <h2 className="title is-4 mt-3">Prywatne archiwum</h2>
                                <p>Bezpieczne przechowywanie nagrań konkursowych i prywatnych treningów z możliwością dzielenia się linkami.</p>
                            </div>
                        </div>
                    </div>

                    <div className="box has-text-centered mt-6">
                        <h2 className="title is-3">Gotowy, aby zacząć?</h2>
                        <p className="subtitle is-5 mb-5">Zaloguj się aby uzyskać dostęp do nagrań swojej grupy</p>
                        <div className="buttons is-centered">
                            <LoginButton signinRedirect={() => authContext.signinRedirect()} />
                        </div>
                    </div>
                </section>

                <div hidden={true}>
                    <p>{envVariables}</p>
                </div>
            </div>
        )
    }

    return (
        <React.Fragment>
            {authContext.isAuthenticated() ? renderAuthenticatedDashboard() : renderLandingPage()}
        </React.Fragment>
    );
};

export default Home;
