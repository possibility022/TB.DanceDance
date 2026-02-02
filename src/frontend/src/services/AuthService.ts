import { Log, User, UserManager, WebStorageStateStore } from "oidc-client-ts";
import ConfigProvider from "./ConfigProvider";

export interface IAuthService extends TokenProvider {
    signinRedirectCallback(): Promise<void>
    getUser(): Promise<User>
    parseJwt(token: string): object
    signinRedirect(): Promise<void>
    navigateToScreen(): void
    isAuthenticated(): boolean
    signinSilent(): Promise<void>
    signinSilentCallback(): Promise<void>
    logout(): Promise<void>
    signoutRedirectCallback(): Promise<void>
}

interface OidcStorage {
    access_token: string
    id_token?: string
}

export interface TokenProvider {
    getAccessToken(): Promise<string | null>
    getAccessTokenNoRefresh(): Promise<string | null>
}

export class AuthService implements IAuthService, TokenProvider {

    userManager: UserManager

    constructor() {
        const identityConfig = ConfigProvider.getIdentityConfig()

        this.userManager = new UserManager(
            {
                ...identityConfig,
                userStore: new WebStorageStateStore({ store: window.localStorage })
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
            try {
                const o = this.parseJwt(token)
                const expireAt = o?.exp as number
                const expire = new Date(expireAt * 1000)

                if (expire < new Date() && refreshTokenIfExpired) {
                    await this.signinSilent()
                    return this.getTokenFromStorage()
                }

                return token
            } catch (error) {
                console.error("Failed to parse access token", error);
                return null;
            }
        }
        else
            return null
    }

    signinRedirectCallback = async () => {
        await this.userManager.signinRedirectCallback();
    };


    getUser = async () => {
        const user = await this.userManager.getUser();
        if (!user) {
            return await this.userManager.signinRedirectCallback();
        }
        return user;
    };

    parseJwt = (token: string) => {
        try {
            const base64Url = token.split(".")[1];
            if (!base64Url) {
                throw new Error("Invalid token format");
            }
            const base64 = base64Url.replaceAll("-", "+").replaceAll("_", "/");
            return JSON.parse(window.atob(base64));
        } catch (error) {
            console.error("Failed to parse JWT token", error);
            throw error;
        }
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

        const identityConfig = ConfigProvider.getIdentityConfig()
        const metadataOidc = ConfigProvider.getMetadataConfig()

        const authEndpoint = metadataOidc.authorization_endpoint;
        const clientId = identityConfig.client_id

        const item = localStorage.getItem(`oidc.user:${authEndpoint}:${clientId}`)
        if (item === null)
            return null

        return JSON.parse(item) as OidcStorage
    }

    getRegisterUri = () => {
        const config = ConfigProvider.getIdentityConfig()
        return config.register + "?returnUrl=" + window.location.href
    }


    isAuthenticated = () => {
        const oidcStorage = this.getOidcStorage()

        if (!oidcStorage)
            return false

        return (!!oidcStorage && !!oidcStorage.access_token)
    };

    signinSilent = async () => {
        let user = await this.userManager.getUser();
        user = await this.userManager.signinSilent(user ?? undefined)
        console.log("signed in", user);
    };

    signinSilentCallback = async () => {
        await this.userManager.signinSilentCallback();
    };

    logout = async () => {
        const oidcStorage = this.getOidcStorage()
        const idToken = oidcStorage?.id_token
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