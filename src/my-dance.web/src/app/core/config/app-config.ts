/**
 * Per-environment runtime configuration.
 *
 * Loaded from `config.json` at startup (see {@link ConfigService}) so the same
 * build can be deployed to multiple environments. Localhost defaults below let a
 * developer run the app with no config file present.
 */
export interface AppConfig {
  /** Base URL of the REST API. */
  readonly apiBaseUrl: string;
  /** OIDC authority / issuer (the auth server base URL). */
  readonly authUrl: string;
  /** OIDC login redirect URI. Defaults to `<origin>/callback`. */
  readonly redirectUri: string;
}

export const DEFAULT_CONFIG: AppConfig = {
  apiBaseUrl: 'https://localhost:7068',
  authUrl: 'https://localhost:7259',
  redirectUri: `${window.location.origin}/callback`,
};
