import React from "react";
import { AuthConsumer } from "../../providers/AuthProvider";
import { IAuthService } from "../../services/AuthService";

export const Logout = () => (
    <AuthConsumer>
        {/*todo - fix linted issue */}
        {/* eslint-disable-next-line @typescript-eslint/unbound-method */}
        {({ logout }: IAuthService) => {
            logout().catch(e => console.error(e));
            return <span>loading</span>;
        }}
    </AuthConsumer>
);