/* eslint-disable @typescript-eslint/no-unsafe-assignment */
/* eslint-disable @typescript-eslint/restrict-plus-operands */

export const REACT_APP_AUTH_URL_TO_REPLACE = 'https://localhost:7068'
export const REACT_APP_REDIRECT_URI_TO_REPLACE = 'http://localhost:3000/callback'

export const IDENTITY_CONFIG: IdentityConfig = {
    authority: REACT_APP_AUTH_URL_TO_REPLACE, //(string): The URL of the OIDC provider.
    client_id: 'tbdancedancefront', //(string): Your client application's identifier as registered with the OIDC provider.
    redirect_uri: REACT_APP_REDIRECT_URI_TO_REPLACE, //The URI of your client application to receive a response from the OIDC provider.
    // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
    login: `${REACT_APP_AUTH_URL_TO_REPLACE}/login`,
    register: `${REACT_APP_AUTH_URL_TO_REPLACE}/account/register`,
    automaticSilentRenew: false, //(boolean, default: false): Flag to indicate if there should be an automatic attempt to renew the access token prior to its expiration.
    loadUserInfo: false, //(boolean, default: true): Flag to control if additional identity data is loaded from the user info endpoint in order to populate the user's profile.
    //silent_redirect_uri: process.env.REACT_APP_SILENT_REDIRECT_URL, //(string): The URL for the page containing the code handling the silent renew.
    //post_logout_redirect_uri: process.env.REACT_APP_LOGOFF_REDIRECT_URL, // (string): The OIDC post-logout redirect URI.
    //audience: "https://example.com", //is there a way to specific the audience when making the jwt
    responseType: "token id_token", //(string, default: 'id_token'): The type of response desired from the OIDC provider.
    grantType: "code",
    scope: "openid tbdancedanceapi.read offline_access", //(string, default: 'openid'): The scope being requested from the OIDC provider.
    webAuthResponseType: "id_token token"
}

export type IdentityConfig = {
    authority: string
    client_id: string
    redirect_uri: string
    login: string
    register: string
    automaticSilentRenew: boolean
    loadUserInfo: boolean
    //silent_redirect_uri: process.env.REACT_APP_SILENT_REDIRECT_URL, //(string): The URL for the page containing the code handling the silent renew.
    //post_logout_redirect_uri: process.env.REACT_APP_LOGOFF_REDIRECT_URL, // (string): The OIDC post-logout redirect URI.
    //audience: "https://example.com", //is there a way to specific the audience when making the jwt
    responseType: string
    grantType: string
    scope: string
    webAuthResponseType: string
}

export const METADATA_OIDC: MetadataOidc = {
    issuer: "https://identityserver",
    jwks_uri: REACT_APP_AUTH_URL_TO_REPLACE + "/.well-known/openid-configuration/jwks",
    authorization_endpoint: REACT_APP_AUTH_URL_TO_REPLACE,
    token_endpoint: REACT_APP_AUTH_URL_TO_REPLACE + "/connect/token",
    userinfo_endpoint: REACT_APP_AUTH_URL_TO_REPLACE + "/connect/userinfo",
    end_session_endpoint: REACT_APP_AUTH_URL_TO_REPLACE + "/connect/endsession",
    check_session_iframe: REACT_APP_AUTH_URL_TO_REPLACE + "/connect/checksession",
    revocation_endpoint: REACT_APP_AUTH_URL_TO_REPLACE + "/connect/revocation",
    introspection_endpoint: REACT_APP_AUTH_URL_TO_REPLACE + "/connect/introspect"
}

export type MetadataOidc = {
    issuer: string
    jwks_uri: string
    authorization_endpoint: string
    token_endpoint: string
    userinfo_endpoint: string
    end_session_endpoint: string
    check_session_iframe: string
    revocation_endpoint: string
    introspection_endpoint: string
}
