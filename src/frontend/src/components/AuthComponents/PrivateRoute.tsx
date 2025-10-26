import React from "react";
import { AuthConsumer } from "../../providers/AuthProvider";
import { IAuthService } from "../../services/AuthService";

interface input {
    element: React.ReactNode | null
}

export const PrivateRoute = ({ element, ...rest }: input) => {
    return(
        <AuthConsumer>
            {({ isAuthenticated, signinRedirect }: IAuthService) => {
                if (!!element && isAuthenticated()) {
                    return element;
                } else {
                    signinRedirect().catch((e) => console.log(e));
                    return <span>loading</span>;
                }
            }}
        </AuthConsumer>
    );
};