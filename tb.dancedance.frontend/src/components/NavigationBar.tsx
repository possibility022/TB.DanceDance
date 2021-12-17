import { useAuth0 } from "@auth0/auth0-react"
import * as React from "react"
import LoginButton from "./LoginButton"
import LogoutButton from "./LogoutButton"


export function NavigationBar() {

    const { user, isAuthenticated, isLoading } = useAuth0()

    return (
        <nav className="navbar" role="navigation" aria-label="main navigation">
            <div className="navbar-brand">
                <a className="navbar-item" href="https://bulma.io">
                    <img src="https://bulma.io/images/bulma-logo.png" width="112" height="28" />
                </a>

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

                            isAuthenticated && (
                                <div>
                                    <h2>{user?.name}</h2>
                                    <p>{user?.email}</p>
                                </div>

                            )}
                    </div>
                    <div className="navbar-item">
                        <div className="buttons">
                            <LoginButton />
                            {isAuthenticated && <LogoutButton />}
                        </div>
                    </div>
                </div>
            </div>
        </nav>
    )
}

export default NavigationBar
