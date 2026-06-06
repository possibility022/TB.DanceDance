import { EnvironmentProviders, inject, makeEnvironmentProviders, provideAppInitializer } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ConfigService } from './config.service';

/**
 * Loads runtime configuration before the app bootstraps, so `ConfigService`
 * and the OIDC config are populated by the time any route or guard runs.
 */
export function provideRuntimeConfig(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideAppInitializer(() => firstValueFrom(inject(ConfigService).load())),
  ]);
}
