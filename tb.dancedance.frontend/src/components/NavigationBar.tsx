import { useAuth } from "oidc-react";
import * as React from "react"
import LoginButton from "./LoginButton"
import LogoutButton from "./LogoutButton"

interface INavigationBarProps {
    userIsSignedIn: boolean
}

export function NavigationBar(props: INavigationBarProps) {

    const auth = useAuth();

    return (
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
                    <a className="navbar-item">
                        Home
                    </a>

                    <a className="navbar-item">
                        Documentation
                    </a>

                    <div className="navbar-item has-dropdown is-hoverable">
                        <a className="navbar-link">
                            More
                        </a>

                        <div className="navbar-dropdown">
                            <a className="navbar-item">
                                About
                            </a>
                            <a className="navbar-item">
                                Jobs
                            </a>
                            <a className="navbar-item">
                                Contact
                            </a>
                        </div>
                    </div>
                </div>

                <div className="navbar-end">
                    <div className="navbar-item">
                        {
                            props.userIsSignedIn &&
                            <h2>{auth.userData?.profile.name} - {auth.userData?.profile.email}</h2>
                        }
                    </div>
                    <div className="navbar-item">
                        <div className="buttons">
                            {!props.userIsSignedIn && <LoginButton />}
                        </div>
                    </div>
                </div>
            </div>
        </nav>
    )
}

export default NavigationBar
