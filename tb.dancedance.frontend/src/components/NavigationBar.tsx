import React from "react"
import { Link, NavLink } from "react-router-dom"
import { LoginLogout } from "./LoginLogoutComponents/LoginLogout"

export function NavigationBar() {
    return (
        <React.Fragment>
            <nav className="navbar" role="navigation" aria-label="main navigation">
                <div className="navbar-brand">
                    <h1 className="title App-logo">
                        Dance Dance
                    </h1>

                    <a role="button" className="navbar-burger" aria-label="menu" aria-expanded="false" data-target="navbarBasicExample">
                        <span aria-hidden="true"></span>
                        <span aria-hidden="true"></span>
                        <span aria-hidden="true"></span>
                    </a>
                </div>

                <div id="navbarBasicExample" className="navbar-menu">
                    <div className="navbar-start">
                        <Link className='navbar-item' to="">Home</Link>
                        <Link className='navbar-item' to="videos">Videos</Link>
                    </div>

                    <div className="navbar-end">
                        <div className="navbar-item">
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
