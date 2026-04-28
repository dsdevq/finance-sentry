import {inject, Injectable, InjectionToken} from '@angular/core';

import {type Provider} from '../../../shared/models/provider/provider.model';
import {type ConnectStrategy} from './connect-strategy';

/**
 * Multi-token: every concrete `ConnectStrategy` registers here.
 * Read by `ConnectStrategyRegistry`; not consumed directly by form components.
 */
export const CONNECT_STRATEGIES = new InjectionToken<readonly ConnectStrategy[]>(
  'ConnectStrategies'
);

/**
 * Singular token consumed by form components inside the connect modal.
 *
 * The connect modal builds a per-form `Injector` at render time that provides
 * the resolved strategy as `CONNECT_STRATEGY`. Form components stay agnostic
 * of which concrete strategy class is behind the token — they call
 * `inject(CONNECT_STRATEGY).submit(payload)` and let the registry decide.
 *
 * No form component should import a concrete strategy class.
 */
export const CONNECT_STRATEGY = new InjectionToken<ConnectStrategy>('ConnectStrategy');

@Injectable()
export class ConnectStrategyRegistry {
  private readonly strategies = inject(CONNECT_STRATEGIES, {optional: true}) ?? [];

  public getBySlug(slug: Provider): ConnectStrategy {
    const found = this.strategies.find(s => s.slug === slug);
    if (!found) {
      throw new Error(`No ConnectStrategy registered for slug "${slug}"`);
    }
    return found;
  }

  public all(): readonly ConnectStrategy[] {
    return this.strategies;
  }
}
