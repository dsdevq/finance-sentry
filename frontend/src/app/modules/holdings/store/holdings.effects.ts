import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {pipe, switchMap, tap} from 'rxjs';

import {StoreErrorUtils} from '../../../shared/utils/store-error.utils';
import {BinanceService} from '../../bank-sync/services/binance.service';
import {IBKRService} from '../../bank-sync/services/ibkr.service';
import {type Position} from '../models/position/position.model';
import {HoldingsService} from '../services/holdings.service';
import {PositionsService} from '../services/positions.service';
import {type HoldingsState} from './holdings.state';

interface StoreMethods {
  setLoading(): void;
  setSummary(summary: HoldingsState['summary'] & {}): void;
  setError(errorCode: Nullable<string>): void;
  setPositionsLoading(): void;
  setPositions(positions: Position[]): void;
  setPositionsError(errorCode: Nullable<string>): void;
}

export function holdingsEffects(store: StoreMethods) {
  const holdingsService = inject(HoldingsService);
  const binanceService = inject(BinanceService);
  const ibkrService = inject(IBKRService);
  const positionsService = inject(PositionsService);

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

  const loadPositions = rxMethod<void>(
    pipe(
      tap(() => store.setPositionsLoading()),
      switchMap(() =>
        positionsService.getPositions().pipe(
          tap(positions => store.setPositions(positions)),
          StoreErrorUtils.catchAndSetError({
            setError: (code: Nullable<string>) => store.setPositionsError(code),
          })
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

  return {load, loadPositions, disconnectBinance, disconnectIBKR};
}

export function holdingsHooks(store: ReturnType<typeof holdingsEffects>) {
  return {
    onInit: () => store.load(),
  };
}
