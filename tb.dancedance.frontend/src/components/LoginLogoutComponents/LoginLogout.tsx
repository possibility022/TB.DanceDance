import * as React from 'react';
import { AuthContext } from '../../providers/AuthProvider';
import LoginButton from './LoginButton';
import LogoutButton from './LogoutButton';

export function LoginLogout() {

    const authContext = React.useContext(AuthContext)

    const content = () => {
        if (authContext.isAuthenticated()) {
            return <LogoutButton singoutRedired={async () => authContext.logout()} />
        } else {
            return <LoginButton signinRedirect={async () => authContext.signinRedirect()} />
        }
    }

    return (
        content()
    );
}

