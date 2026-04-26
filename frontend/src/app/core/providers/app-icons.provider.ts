import {type EnvironmentProviders} from '@angular/core';
import {provideCustomIcons} from '@dsdevq-common/ui';

export function provideAppIcons(): EnvironmentProviders {
  return provideCustomIcons({
    urls: {
      // eslint-disable-next-line @typescript-eslint/naming-convention
      'provider-plaid': '/assets/providers/plaid.svg',
      // eslint-disable-next-line @typescript-eslint/naming-convention
      'provider-monobank': '/assets/providers/monobank.svg',
      // eslint-disable-next-line @typescript-eslint/naming-convention
      'provider-binance': '/assets/providers/binance.svg',
      // eslint-disable-next-line @typescript-eslint/naming-convention
      'provider-ibkr': '/assets/providers/ibkr.svg',
    },
  });
}
