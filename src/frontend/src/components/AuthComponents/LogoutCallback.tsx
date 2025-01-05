import React from "react";
import { AuthConsumer } from "../../providers/AuthProvider";
import { IAuthService } from "../../services/AuthService";

export const LogoutCallback = () => (
    <AuthConsumer>
        {/* eslint-disable-next-line @typescript-eslint/unbound-method */}
        {({ signoutRedirectCallback }: IAuthService) => {
            signoutRedirectCallback().catch(e => console.error(e));
            return <span>loading</span>;
        }}
    </AuthConsumer>
);