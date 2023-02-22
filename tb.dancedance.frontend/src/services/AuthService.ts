import { Log, User, UserManager, WebStorageStateStore } from "oidc-client-ts";

import { IDENTITY_CONFIG, METADATA_OIDC, replaceValues } from "../authConst";

export interface IAuthService extends TokenProvider {
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

interface OidcStorage {
    access_token: string
}

export interface TokenProvider {
    getAccessToken(): Promise<string | null>
    getAccessTokenNoRefresh(): Promise<string | null>
}

export class AuthService implements IAuthService, TokenProvider {

    userManager: UserManager

    constructor() {

        // todo
        const identityConfig = replaceValues(IDENTITY_CONFIG)
        if (!identityConfig)
            throw new Error("Something wrong with identity config. It is null or undefined");

        this.userManager = new UserManager(
            {
                ...identityConfig,
                userStore: new WebStorageStateStore({ store: window.sessionStorage })
            }
        )

        Log.setLogger(console)
        Log.setLevel(Log.DEBUG)

        this.userManager.events.addUserLoaded(() => {
            if (window.location.href.indexOf("signin-oidc") !== -1) {
                this.navigateToScreen();
            }
        });
        this.userManager.events.addSilentRenewError((e) => {
            console.log("silent renew error", e.message);
        });

        this.userManager.events.addAccessTokenExpired(() => {
            console.log("token expired");
            this.signinSilent()
                .catch(e => console.log(e));
        });
    }

    getAccessTokenNoRefresh = (): Promise<string | null> => {
        return this.getAccessTokenInternal(false)
    }

    getAccessToken = (): Promise<string | null> => {
        return this.getAccessTokenInternal(true)
    }

    private getTokenFromStorage = (): string | null => {
        const oidcStorage = this.getOidcStorage()
        return oidcStorage?.access_token ?? null
    }

    private getAccessTokenInternal = async (refreshTokenIfExpired: boolean): Promise<string | null> => {
        const token = this.getTokenFromStorage()
        if (token) {
            // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
            const o = this.parseJwt(token)
            // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
            const expireAt = o?.exp as number
            const expire = new Date(expireAt * 1000)

            if (expire < new Date() && refreshTokenIfExpired) {
                await this.signinSilent()
                return this.getTokenFromStorage()
            }

            return token
        }
        else
            return null
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

    private getOidcStorage = (): OidcStorage | null => {

        // this is not working due to:
        // https://stackoverflow.com/a/70452191
        // https://github.com/facebook/create-react-app/issues/11773
        // if (!process.env.REACT_APP_AUTH_URL)
        //     throw new Error("SETTINGS NOT CONFIGURED")
        // if (!process.env.REACT_APP_IDENTITY_CLIENT_ID)
        //     throw new Error("SETTINGS NOT CONFIGURED")

        // todo
        const identityConfig = replaceValues(IDENTITY_CONFIG)
        if (!identityConfig)
            throw new Error("Something wrong with identity config. It is null or undefined");

        const metadataOidc = replaceValues(METADATA_OIDC)
        if (!metadataOidc)
            throw new Error("Something wrong with identity config. It is null or undefined");

        const authEndpoint = metadataOidc.authorization_endpoint;
        const clientId = identityConfig.client_id

        const item = sessionStorage.getItem(`oidc.user:${authEndpoint}:${clientId}`)
        if (item === null)
            return null

        // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
        const oidcStorage = JSON.parse(item) as OidcStorage
        return oidcStorage
    }


    isAuthenticated = () => {
        const oidcStorage = this.getOidcStorage()

        if (!oidcStorage)
            return false

        return (!!oidcStorage && !!oidcStorage.access_token)
    };

    signinSilent = async () => {
        const user = await this.userManager.signinSilent()
        console.log("signed in", user);
    };

    signinSilentCallback = async () => {
        await this.userManager.signinSilentCallback();
    };

    logout = async () => {
        const idToken = localStorage.getItem("id_token")
        const requestArgs = {
            id_token_hint: idToken ?? undefined
        }
        await this.userManager.clearStaleState();
        await this.userManager.signoutRedirect(requestArgs);
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