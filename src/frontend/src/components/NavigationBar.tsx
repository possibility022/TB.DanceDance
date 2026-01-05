import React, { useState } from "react"
import { Link } from "react-router-dom"
import { LoginLogout } from "./LoginLogoutComponents/LoginLogout"
import ConfigProvider from "../services/ConfigProvider";

export function NavigationBar() {

    const onMenuClick = () => {
        const element = document.getElementById('navbarBasicExample');
        const burgetButton = document.getElementById('navbar-burger-button');
        element?.classList.toggle('is-active')
        burgetButton?.classList.toggle('is-active')
    }

    return (
        <React.Fragment>
            <nav className="navbar" role="navigation" aria-label="main navigation">
                <div className="navbar-brand">
                    <Link to={'/'}>
                    <h1 className="title App-logo">
                        Dance Dance
                    </h1>
                    </Link>

                    <a role="button" id='navbar-burger-button' className="navbar-burger" aria-label="menu" aria-expanded="false" onClick={onMenuClick} data-target="navbarBasicExample">
                        <span aria-hidden="true"></span>
                        <span aria-hidden="true"></span>
                        <span aria-hidden="true"></span>
                    </a>
                </div>

                <div id="navbarBasicExample" className="navbar-menu">
                    <div className="navbar-start">
                        <Link className='navbar-item' to="events">Nagrania z Wydarzeń</Link>
                        <Link className='navbar-item' to="videos">Nagrania z Zajęć Regularnych</Link>
                        <Link className='navbar-item' to="videos/my">Prywatne Nagrania</Link>
                    </div>

                    <div className="navbar-end">
                        <div className="navbar-item has-dropdown is-hoverable">
                            <Link className='navbar-item is-size-7' to="videos">Administracja Dostępem</Link>
                            <div className="navbar-dropdown">
                                <Link className='navbar-item' to="access/requestedaccesses">Zarządzanie Przypisaniem Grup i Dostępem do Wydarzeń</Link>
                                <Link className='navbar-item' to="videos/requestassignment">Uzyskaj Dostęp</Link>
                            </div>
                        </div>

                        <div className="navbar-item has-dropdown is-hoverable">
                            <Link className='navbar-item is-size-7' to="videos/upload">Wyślij Nagranie</Link>
                        </div>


                        <div className="navbar-item">
                            <a className='navbar-item is-size-7' href={ConfigProvider.getIdentityConfig().authority + '/policy/dancedanceapp'}>Polityka Prywatności</a>
                            <div className="buttons">
                                <LoginLogout />
                            </div>
                        </div>
                    </div>
                </div>
            </nav>
        </React.Fragment>
    )
}

export default NavigationBar
