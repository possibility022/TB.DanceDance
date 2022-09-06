import { User } from "oidc-client";
import React, { Component } from "react";
import { AuthService, IAuthService } from "../services/AuthService";


class DummyIAuthService implements IAuthService{
    signinRedirectCallback(): Promise<void> {
        throw new Error("Method not implemented.");
    }
    getUser(): Promise<User> {
        throw new Error("Method not implemented.");
    }
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
    signinSilent(): void {
        throw new Error("Method not implemented.");
    }
    signinSilentCallback(): Promise<void> {
        throw new Error("Method not implemented.");
    }
    createSigninRequest(): void {
        throw new Error("Method not implemented.");
    }
    logout(): Promise<void> {
        throw new Error("Method not implemented.");
    }
    signoutRedirectCallback(): Promise<void> {
        throw new Error("Method not implemented.");
    }

}

const AuthContext = React.createContext<IAuthService>(new DummyIAuthService());

export const AuthConsumer = AuthContext.Consumer;

export class AuthProvider extends Component {
    authService;
    // eslint-disable-next-line @typescript-eslint/ban-types
    constructor(props: {} | Readonly<{}>) {
        super(props);
        this.authService = new AuthService();
    }
    render() {
        return <AuthContext.Provider value={this.authService}>{this.props.children}</AuthContext.Provider>;
    }
}