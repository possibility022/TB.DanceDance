import React from "react";
import { AuthConsumer } from "../../providers/AuthProvider";
import { IAuthService } from "../../services/AuthService";

interface input {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    element: React.ReactNode | null
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export const PrivateRoute = ({ element, ...rest }: input) => {
    return(
        <AuthConsumer>
            {/* eslint-disable-next-line @typescript-eslint/unbound-method */}
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