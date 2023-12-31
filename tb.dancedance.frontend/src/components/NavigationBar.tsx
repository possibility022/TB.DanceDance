import React, { useState } from "react"
import { Link } from "react-router-dom"
import { authService } from "../providers/AuthProvider";
import appClient from "../services/AppClient";
import { Button } from "./Button";
import { LoginLogout } from "./LoginLogoutComponents/LoginLogout"
import ConfigProvider from "../services/ConfigProvider";

export function NavigationBar() {

    const [registrationIsWaiting, setRegistrationIsWaiting] = useState(false)

    const onMenuClick = () => {
        const element = document.getElementById('navbarBasicExample');
        const burgetButton = document.getElementById('navbar-burger-button');
        element?.classList.toggle('is-active')
        burgetButton?.classList.toggle('is-active')
    }

    const registerAction = async () => {
        setRegistrationIsWaiting(true)
        await appClient.warmupRequest()
        setRegistrationIsWaiting(false)
        window.location.href = authService.getRegisterUri()
    }

    return (
        <React.Fragment>
            <nav className="navbar" role="navigation" aria-label="main navigation">
                <div className="navbar-brand">
                    <h1 className="title App-logo">
                        Dance Dance
                    </h1>

                    <a role="button" id='navbar-burger-button' className="navbar-burger" aria-label="menu" aria-expanded="false" onClick={onMenuClick} data-target="navbarBasicExample">
                        <span aria-hidden="true"></span>
                        <span aria-hidden="true"></span>
                        <span aria-hidden="true"></span>
                    </a>
                </div>

                <div id="navbarBasicExample" className="navbar-menu">
                    <div className="navbar-start">
                        <Link className='navbar-item' to="events">Wydarzenia</Link>
                        <div className="navbar-item has-dropdown is-hoverable">
                            <Link className='navbar-item' to="videos">Nagrania</Link>
                            <div className="navbar-dropdown">
                                <Link className='navbar-item' to="videos">Nagrania</Link>
                                <Link className='navbar-item' to="videos/requestassignment">Uzyskaj Dostęp</Link>
                                <Link className='navbar-item' to="videos/upload">Wyślij Nagranie</Link>
                            </div>
                        </div>
                    </div>

                    <div className="navbar-end">
                        <div className="navbar-item">
                            <a className='navbar-item is-size-7' href={ConfigProvider.getIdentityConfig().authority + '/policy/dancedanceapp'}>Polityka Prywatności</a>
                            <div className="buttons">
                                <LoginLogout />
                                <Button
                                    isLoading={registrationIsWaiting}
                                    onClick={() => {
                                        registerAction()
                                            .catch(e => console.error(e))
                                            .finally(() => setRegistrationIsWaiting(false))
                                    }}>
                                    Register
                                </Button>
                            </div>
                        </div>
                    </div>
                </div>
            </nav>
        </React.Fragment>
    )
}

export default NavigationBar
