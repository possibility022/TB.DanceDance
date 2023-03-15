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
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const c: any = { ...input }
        let autUrl = process.env.REACT_APP_AUTH_URL as string

        if (!autUrl) {
            autUrl = REACT_APP_AUTH_URL_TO_REPLACE
        }

        let redirectUri = process.env.REACT_APP_REDIRECT_URI
        if (!redirectUri) {
            redirectUri = REACT_APP_REDIRECT_URI_TO_REPLACE
        }

        for (const key in c) {
            // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access, @typescript-eslint/no-unsafe-assignment
            const v = c[key]
            if (typeof v === 'string') {
                // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
                c[key] = v.replaceAll(REACT_APP_AUTH_URL_TO_REPLACE, autUrl).replaceAll(REACT_APP_REDIRECT_URI_TO_REPLACE, redirectUri)
            }
        }

        // eslint-disable-next-line @typescript-eslint/no-unsafe-return
        return c as T
    }
}