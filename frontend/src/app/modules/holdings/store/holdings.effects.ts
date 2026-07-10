import {inject} from '@angular/core';
import {toObservable} from '@angular/core/rxjs-interop';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {pipe, skip, switchMap, tap} from 'rxjs';

import {StoreErrorUtils} from '../../../shared/utils/store-error.utils';
import {AccountsStore} from '../../bank-sync/store/accounts/accounts.store';
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

  return {load, loadPositions};
}

export function holdingsHooks(store: ReturnType<typeof holdingsEffects>) {
  return {
    onInit: () => {
      store.load();
      // Disconnect actions live exclusively in AccountsStore; refresh the
      // holdings summary whenever it reports a completed disconnect. Optional
      // so the store keeps working (minus the reaction) if a host page does
      // not provide AccountsStore.
      const accountsStore = inject(AccountsStore, {optional: true});
      if (accountsStore) {
        rxMethod<number>(
          pipe(
            skip(1),
            tap(() => store.load())
          )
        )(toObservable(accountsStore.disconnectVersion));
      }
    },
  };
}
