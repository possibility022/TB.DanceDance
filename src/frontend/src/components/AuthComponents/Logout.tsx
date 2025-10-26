import React from "react";
import { AuthConsumer } from "../../providers/AuthProvider";
import { IAuthService } from "../../services/AuthService";

export const Logout = () => (
    <AuthConsumer>
        {({ logout }: IAuthService) => {
            logout().catch(e => console.error(e));
            return <span>loading</span>;
        }}
    </AuthConsumer>
);