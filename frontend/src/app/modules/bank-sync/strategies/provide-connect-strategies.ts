import {type EnvironmentProviders, makeEnvironmentProviders} from '@angular/core';

import {BinanceConnectStrategy} from './binance.strategy';
import {CONNECT_STRATEGIES, ConnectStrategyRegistry} from './connect-strategy.token';
import {IbkrConnectStrategy} from './ibkr.strategy';
import {MonobankConnectStrategy} from './monobank.strategy';
import {PlaidConnectStrategy} from './plaid.strategy';

export function provideConnectStrategies(): EnvironmentProviders {
  return makeEnvironmentProviders([
    ConnectStrategyRegistry,
    {provide: CONNECT_STRATEGIES, multi: true, useExisting: PlaidConnectStrategy},
    {provide: CONNECT_STRATEGIES, multi: true, useExisting: MonobankConnectStrategy},
    {provide: CONNECT_STRATEGIES, multi: true, useExisting: BinanceConnectStrategy},
    {provide: CONNECT_STRATEGIES, multi: true, useExisting: IbkrConnectStrategy},
  ]);
}
