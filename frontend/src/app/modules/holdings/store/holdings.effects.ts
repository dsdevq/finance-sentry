import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {pipe, switchMap, tap} from 'rxjs';

import {StoreErrorUtils} from '../../../shared/utils/store-error.utils';
import {BinanceService} from '../../bank-sync/services/binance.service';
import {IBKRService} from '../../bank-sync/services/ibkr.service';
import {HoldingsService} from '../services/holdings.service';
import {type HoldingsState} from './holdings.state';

interface StoreMethods {
  setLoading(): void;
  setSummary(summary: HoldingsState['summary'] & {}): void;
  setError(errorCode: Nullable<string>): void;
}

export function holdingsEffects(store: StoreMethods) {
  const holdingsService = inject(HoldingsService);
  const binanceService = inject(BinanceService);
  const ibkrService = inject(IBKRService);

  const load = rxMethod<void>(
    pipe(
      tap(() => store.setLoading()),
      switchMap(() =>
        holdingsService.getSummary().pipe(
          tap(summary => store.setSummary(summary)),
          StoreErrorUtils.catchAndSetError(store)
        )
      )
    )
  );

  const disconnectBinance = rxMethod<void>(
    pipe(
      switchMap(() =>
        binanceService.disconnect().pipe(
          tap(() => load()),
          StoreErrorUtils.catchAndSetError(store)
        )
      )
    )
  );

  const disconnectIBKR = rxMethod<void>(
    pipe(
      switchMap(() =>
        ibkrService.disconnect().pipe(
          tap(() => load()),
          StoreErrorUtils.catchAndSetError(store)
        )
      )
    )
  );

  return {load, disconnectBinance, disconnectIBKR};
}

export function holdingsHooks(store: ReturnType<typeof holdingsEffects>) {
  return {
    onInit: () => store.load(),
  };
}
