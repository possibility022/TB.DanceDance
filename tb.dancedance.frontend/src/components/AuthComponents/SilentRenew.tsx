import React from "react";
import { AuthConsumer } from "../../providers/AuthProvider";
import { IAuthService } from "../../services/AuthService";

export const SilentRenew = () => (
    <AuthConsumer>
        {/* eslint-disable-next-line @typescript-eslint/unbound-method */}
        {({ signinSilentCallback }: IAuthService) => {
            signinSilentCallback().catch(e => console.error(e));
            return <span>loading</span>;
        }}
    </AuthConsumer>
);