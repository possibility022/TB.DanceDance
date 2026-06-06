import { EnvironmentProviders } from '@angular/core';
import {
  LogLevel,
  OpenIdConfiguration,
  StsConfigHttpLoader,
  StsConfigLoader,
  provideAuth,
} from 'angular-auth-oidc-client';
import { map } from 'rxjs';

import { ConfigService } from '../config/config.service';

/** Fixed OIDC contract — see docs/angular-rewrite/01-authentication-and-authorization.md. */
export const OIDC_CLIENT_ID = 'tbdancedancefront';
export const OIDC_SCOPE = 'openid tbdancedanceapi.read offline_access profile';

/**
 * Builds the OIDC config from runtime configuration. Authority and API URL are
 * environment-specific; everything else is the stable auth contract.
 */
function authConfigLoaderFactory(config: ConfigService): StsConfigLoader {
  const origin = window.location.origin;
  const config$ = config.load().pipe(
    map(
      (cfg): OpenIdConfiguration => ({
        authority: cfg.authUrl,
        clientId: OIDC_CLIENT_ID,
        redirectUrl: cfg.redirectUri,
        postLogoutRedirectUri: `${origin}/logout/callback`,
        silentRenewUrl: `${origin}/silentrenew`,
        scope: OIDC_SCOPE,
        responseType: 'code',
        useRefreshToken: true,
        silentRenew: true,
        renewTimeBeforeTokenExpiresInSeconds: 30,
        ignoreNonceAfterRefresh: true,
        autoUserInfo: false,
        // Attach the bearer token only to API calls (handled by authInterceptor()).
        secureRoutes: [cfg.apiBaseUrl],
        logLevel: LogLevel.Warn,
      }),
    ),
  );
  return new StsConfigHttpLoader(config$);
}

/** Configures angular-auth-oidc-client from runtime configuration. */
export function provideAuthentication(): EnvironmentProviders {
  return provideAuth({
    loader: {
      provide: StsConfigLoader,
      useFactory: authConfigLoaderFactory,
      deps: [ConfigService],
    },
  });
}
