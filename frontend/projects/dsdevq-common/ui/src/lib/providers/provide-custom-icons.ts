import {type EnvironmentProviders, inject, makeEnvironmentProviders, provideAppInitializer} from '@angular/core';

import {CmnIconRegistry} from '../services/icon-registry/icon-registry.service';

export interface CustomIconsConfig {
  readonly inline?: Readonly<Record<string, string>>;
  readonly urls?: Readonly<Record<string, string>>;
}

export function provideCustomIcons(config: CustomIconsConfig): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideAppInitializer(() => {
      const registry = inject(CmnIconRegistry);
      if (config.inline) {
        registry.registerInlineMap(config.inline);
      }
      if (config.urls) {
        registry.registerUrlMap(config.urls);
      }
    }),
  ]);
}
