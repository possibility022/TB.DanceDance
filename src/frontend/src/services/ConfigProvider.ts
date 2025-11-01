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
    
    public static async validatePrimaryHostIsAvailable(){
        const primaryHost = this.resolvePrimaryHost()
        const url = METADATA_OIDC.jwks_uri.replace(REACT_APP_AUTH_URL_TO_REPLACE, primaryHost)
        try {
            const res = await fetch(url, {
                method: 'GET',
            })
            return res.status === 200
        } catch (e) {
            console.error(e)
            return false
        }
    }
    
    private static readonly storageKey = "lastFailForMainServer";
    public static useFailoverHost(){
        localStorage.setItem(this.storageKey, Date.now().toString())
    }
    
    public static failoverServerShouldBeUsed(){
        const lastFail = localStorage.getItem(this.storageKey)
        if (lastFail){
            try {
                const lastFailAsInt = parseInt(lastFail)
                if (lastFailAsInt + 1000* 1000* 60> Date.now())
                {
                    return true
                } else {
                    localStorage.removeItem(this.storageKey)
                }
            } catch (e) {
                console.error(e)
            }
        }
        return false
    }
    
    private static resolvePrimaryHost(){
        let autUrl = process.env.REACT_APP_AUTH_URL
        if (!autUrl)
            autUrl = REACT_APP_AUTH_URL_TO_REPLACE
        
        return autUrl
    }
    
    public static resolveApiHost(){
        let baseUrl = process.env.REACT_APP_API_BASE_URL
        let backupUrl = process.env.REACT_APP_API_BASE_URL_BACKUP
        
        if (this.failoverServerShouldBeUsed() && backupUrl)
        {
            baseUrl = backupUrl
        }

        if (!baseUrl)
        {
            baseUrl = 'https://localhost:7068'
        }
        
        return baseUrl
    }

    // todo get rid of this
    private static replaceValues<T>(input: T) {
        const c: any = { ...input }
        let autUrl = this.resolvePrimaryHost()
        let backupUrl = process.env.REACT_APP_AUTH_URL_BACKUP ?? 'https://ddapi.tomb.my.id'

        if (this.failoverServerShouldBeUsed() && backupUrl){
            autUrl = backupUrl
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