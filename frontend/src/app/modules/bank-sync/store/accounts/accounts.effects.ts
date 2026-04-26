import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {pipe, switchMap, tap} from 'rxjs';

import {type WealthSummaryResponse} from '../../../../shared/models/wealth/wealth.model';
import {StoreErrorUtils} from '../../../../shared/utils/store-error.utils';
import {BankSyncService} from '../../services/bank-sync.service';
import {WealthService} from '../../services/wealth.service';

interface EffectsStore {
  setLoading: () => void;
  setSummary: (summary: WealthSummaryResponse) => void;
  setError: (errorCode: Nullable<string>) => void;
}

export function accountsEffects(store: EffectsStore) {
  const wealthService = inject(WealthService);
  const bankSyncService = inject(BankSyncService);

  const load = rxMethod<void>(
    pipe(
      tap(() => store.setLoading()),
      switchMap(() =>
        wealthService.getSummary().pipe(
          tap(summary => store.setSummary(summary)),
          StoreErrorUtils.catchAndSetError(store)
        )
      )
    )
  );

  const disconnectMonobank = rxMethod<void>(
    pipe(
      switchMap(() =>
        bankSyncService.disconnectMonobank().pipe(
          tap(() => load()),
          StoreErrorUtils.catchAndSetError(store)
        )
      )
    )
  );

  const disconnectAccount = rxMethod<string>(
    pipe(
      switchMap(accountId =>
        bankSyncService.disconnectAccount(accountId).pipe(
          tap(() => load()),
          StoreErrorUtils.catchAndSetError(store)
        )
      )
    )
  );

  return {load, disconnectMonobank, disconnectAccount};
}

interface HookStore {
  load: () => void;
}

export function accountsHooks(store: HookStore): void {
  store.load();
}
