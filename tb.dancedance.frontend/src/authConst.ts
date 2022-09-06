/* eslint-disable @typescript-eslint/restrict-plus-operands */

const REACT_APP_AUTH_URL = 'https://localhost:7068'

export const IDENTITY_CONFIG = {
    authority: REACT_APP_AUTH_URL, //(string): The URL of the OIDC provider.
    client_id: 'tbdancedancefront', //(string): Your client application's identifier as registered with the OIDC provider.
    redirect_uri: 'http://localhost:3000/callback', //The URI of your client application to receive a response from the OIDC provider.
    // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
    login: `${REACT_APP_AUTH_URL}/login`,
    automaticSilentRenew: false, //(boolean, default: false): Flag to indicate if there should be an automatic attempt to renew the access token prior to its expiration.
    loadUserInfo: false, //(boolean, default: true): Flag to control if additional identity data is loaded from the user info endpoint in order to populate the user's profile.
    //silent_redirect_uri: process.env.REACT_APP_SILENT_REDIRECT_URL, //(string): The URL for the page containing the code handling the silent renew.
    //post_logout_redirect_uri: process.env.REACT_APP_LOGOFF_REDIRECT_URL, // (string): The OIDC post-logout redirect URI.
    //audience: "https://example.com", //is there a way to specific the audience when making the jwt
    responseType: "token id_token", //(string, default: 'id_token'): The type of response desired from the OIDC provider.
    grantType: "code",
    scope: "openid", //(string, default: 'openid'): The scope being requested from the OIDC provider.
    webAuthResponseType: "id_token token"
};

export const METADATA_OIDC = {
    issuer: "https://identityserver",
    jwks_uri: REACT_APP_AUTH_URL + "/.well-known/openid-configuration/jwks",
    authorization_endpoint: REACT_APP_AUTH_URL,
    token_endpoint: REACT_APP_AUTH_URL + "/connect/token",
    userinfo_endpoint: REACT_APP_AUTH_URL + "/connect/userinfo",
    end_session_endpoint: REACT_APP_AUTH_URL + "/connect/endsession",
    check_session_iframe: REACT_APP_AUTH_URL + "/connect/checksession",
    revocation_endpoint: REACT_APP_AUTH_URL + "/connect/revocation",
    introspection_endpoint: REACT_APP_AUTH_URL + "/connect/introspect"
};