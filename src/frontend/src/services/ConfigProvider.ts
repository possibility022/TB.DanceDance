import { IDENTITY_CONFIG, METADATA_OIDC, REACT_APP_AUTH_URL_TO_REPLACE, REACT_APP_REDIRECT_URI_TO_REPLACE } from "../authConst";

export default class ConfigProvider {

    public static getIdentityConfig = () => {
        const identityConfig = this.replaceValues(IDENTITY_CONFIG)
        if (!identityConfig)
            throw new Error("Something wrong with identity config. It is null or undefined");
        return identityConfig
    }

    public static getMetadataConfig = () => {
        const metadataOidc = this.replaceValues(METADATA_OIDC)
        if (!metadataOidc)
            throw new Error("Something wrong with identity config. It is null or undefined");

        return metadataOidc
    }


    // todo get rid of this
    private static replaceValues<T>(input: T) {
        const c: any = { ...input }
        let autUrl = process.env.REACT_APP_AUTH_URL as string

        if (!autUrl) {
            autUrl = REACT_APP_AUTH_URL_TO_REPLACE
        }

        let redirectUri = process.env.REACT_APP_REDIRECT_URI
        if (!redirectUri) {
            redirectUri = window.origin + "/callback"
        }

        for (const key in c) {
            const v = c[key]
            if (typeof v === 'string') {
                c[key] = v.replaceAll(REACT_APP_AUTH_URL_TO_REPLACE, autUrl).replaceAll(REACT_APP_REDIRECT_URI_TO_REPLACE, redirectUri)
            }
        }

        return c as T
    }
}