import { Log, User, UserManager, WebStorageStateStore } from "oidc-client-ts";

import { IDENTITY_CONFIG, METADATA_OIDC } from "../authConst";

export interface IAuthService {
    signinRedirectCallback(): Promise<void>
    getUser(): Promise<User>
    parseJwt(token: string): object
    signinRedirect(): Promise<void>
    navigateToScreen(): void
    isAuthenticated(): boolean
    signinSilent(): void
    signinSilentCallback(): Promise<void>
    logout(): Promise<void>
    signoutRedirectCallback(): Promise<void>
}

export class AuthService implements IAuthService {

    userManager: UserManager

    constructor() {
        this.userManager = new UserManager(
            {
                ...IDENTITY_CONFIG,
                userStore: new WebStorageStateStore({ store: window.sessionStorage })
            }
        )

        Log.setLogger(console)
        Log.setLevel(Log.DEBUG)

        this.userManager.events.addUserLoaded((user) => {
            if (window.location.href.indexOf("signin-oidc") !== -1) {
                this.navigateToScreen();
            }
        });
        this.userManager.events.addSilentRenewError((e) => {
            console.log("silent renew error", e.message);
        });

        this.userManager.events.addAccessTokenExpired(() => {
            console.log("token expired");
            this.signinSilent();
        });
    }

    signinRedirectCallback = async () => {
        await this.userManager.signinRedirectCallback().then(() => {
            ""; //why this way? todo: check
        });
    };


    getUser = async () => {
        const user = await this.userManager.getUser();
        if (!user) {
            return await this.userManager.signinRedirectCallback();
        }
        return user;
    };

    parseJwt = (token: string) => {
        const base64Url = token.split(".")[1];
        const base64 = base64Url.replace("-", "+").replace("_", "/");
        // eslint-disable-next-line @typescript-eslint/no-unsafe-return
        return JSON.parse(window.atob(base64));
    };


    signinRedirect = async () => {
        localStorage.setItem("redirectUri", window.location.pathname);
        await this.userManager.signinRedirect();
    };


    navigateToScreen = () => {
        window.location.replace("/en/dashboard");
    };


    isAuthenticated = () => {

        // this is not working due to:
        // https://stackoverflow.com/a/70452191
        // https://github.com/facebook/create-react-app/issues/11773
        // if (!process.env.REACT_APP_AUTH_URL)
        //     throw new Error("SETTINGS NOT CONFIGURED")
        // if (!process.env.REACT_APP_IDENTITY_CLIENT_ID)
        //     throw new Error("SETTINGS NOT CONFIGURED")

        const authEndpoint = METADATA_OIDC.authorization_endpoint;
        const clientId = IDENTITY_CONFIG.client_id

        const item = sessionStorage.getItem(`oidc.user:${authEndpoint}:${clientId}`)
        if (item === null)
            return false

        // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
        const oidcStorage = JSON.parse(item)
        
        // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
        return (!!oidcStorage && !!oidcStorage.access_token)
    };

    signinSilent = () => {
        this.userManager.signinSilent()
            .then((user) => {
                console.log("signed in", user);
            })
            .catch((err) => {
                console.log(err);
            });
    };

    signinSilentCallback = async () => {
        await this.userManager.signinSilentCallback();
    };

    logout = async () => {
        const idToken = localStorage.getItem("id_token")
        await this.userManager.signoutRedirect({
            id_token_hint: idToken ?? undefined
        });
        await this.userManager.clearStaleState();
    };

    signoutRedirectCallback = async () => {
        await this.userManager.signoutRedirectCallback().then(() => {
            localStorage.clear();
            if (process.env.REACT_APP_PUBLIC_URL)
                window.location.replace(process.env.REACT_APP_PUBLIC_URL);
        });
        await this.userManager.clearStaleState();
    };

}