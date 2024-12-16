import { User } from "oidc-client-ts";
import React from "react";
import { AuthService, IAuthService } from "../services/AuthService";


class DummyIAuthService implements IAuthService{
    getAccessTokenNoRefresh(): Promise<string | null> {
        throw new Error("Method not implemented.");
    }
    signinRedirectCallback(): Promise<void> {
        throw new Error("Method not implemented.");
    }
    getUser(): Promise<User> {
        throw new Error("Method not implemented.");
    }
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    parseJwt(token: string): object {
        throw new Error("Method not implemented.");
    }
    signinRedirect(): Promise<void> {
        throw new Error("Method not implemented.");
    }
    navigateToScreen(): void {
        throw new Error("Method not implemented.");
    }
    isAuthenticated(): boolean {
        throw new Error("Method not implemented.");
    }
    signinSilent(): Promise<void> {
        throw new Error("Method not implemented.");
    }
    signinSilentCallback(): Promise<void> {
        throw new Error("Method not implemented.");
    }
    logout(): Promise<void> {
        throw new Error("Method not implemented.");
    }
    signoutRedirectCallback(): Promise<void> {
        throw new Error("Method not implemented.");
    }
    getAccessToken(): Promise<string | null> {
        throw new Error("Method not implemented.");
    }

}

export const AuthContext = React.createContext<IAuthService>(new DummyIAuthService());
export const authService = new AuthService()

export const AuthConsumer = AuthContext.Consumer;