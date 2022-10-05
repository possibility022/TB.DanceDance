import * as React from 'react';
import { AuthConsumer } from '../../providers/AuthProvider';
import { IAuthService } from '../../services/AuthService';
import LoginButton from './LoginButton';
import LogoutButton from './LogoutButton';

export function LoginLogout() {
    return (
        <AuthConsumer>
            {({ isAuthenticated, signinRedirect, logout }: IAuthService) => {
                if (isAuthenticated()) {
                    return <LogoutButton singoutRedired={logout} />
                } else {
                    return <LoginButton signinRedirect={signinRedirect} />
                }
            }
            }
        </AuthConsumer>
    );
}

