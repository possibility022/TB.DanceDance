import React, { useContext, useEffect } from "react";
import { AuthContext } from "../../providers/AuthProvider";

interface input {
    element: React.ReactNode | null
}

export const PrivateRoute = ({ element }: input) => {
    const authContext = useContext(AuthContext)
    const [hasRedirected, setHasRedirected] = React.useState(false)

    useEffect(() => {
        if (!authContext.isAuthenticated() && !hasRedirected) {
            setHasRedirected(true)
            authContext.signinRedirect().catch((e) => console.error(e));
        }
    }, [authContext, hasRedirected]);

    if (authContext.isAuthenticated() && element) {
        return <>{element}</>;
    }

    return <span>loading</span>;
};