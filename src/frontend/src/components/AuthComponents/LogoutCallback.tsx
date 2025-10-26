import React from "react";
import { AuthConsumer } from "../../providers/AuthProvider";
import { IAuthService } from "../../services/AuthService";

export const LogoutCallback = () => (
    <AuthConsumer>
        {({ signoutRedirectCallback }: IAuthService) => {
            signoutRedirectCallback().catch(e => console.error(e));
            return <span>loading</span>;
        }}
    </AuthConsumer>
);