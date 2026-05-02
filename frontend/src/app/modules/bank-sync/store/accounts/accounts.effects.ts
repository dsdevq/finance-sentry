import {inject} from '@angular/core';
import {extractErrorCode} from '@dsdevq-common/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, pipe, switchMap, tap} from 'rxjs';

import {type WealthSummaryResponse} from '../../../../shared/models/wealth/wealth.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {WealthService} from '../../services/wealth.service';

interface EffectsStore {
  setLoading: () => void;
  setSuccess: () => void;
  setError: (errorCode: Nullable<string>) => void;
  setSummary: (summary: WealthSummaryResponse) => void;
}

export function accountsEffects(store: EffectsStore) {
  const wealthService = inject(WealthService);
  const bankSyncService = inject(BankSyncService);

  const load = rxMethod<void>(
    pipe(
      tap(() => store.setLoading()),
      switchMap(() =>
        wealthService.getSummary().pipe(
          tap(summary => {
            store.setSummary(summary);
            store.setSuccess();
          }),
          catchError((err: unknown) => {
            store.setError(extractErrorCode(err));
            return EMPTY;
          })
        )
      )
    )
  );

  const disconnectMonobank = rxMethod<void>(
    pipe(
      switchMap(() =>
        bankSyncService.disconnectMonobank().pipe(
          tap(() => load()),
          catchError((err: unknown) => {
            store.setError(extractErrorCode(err));
            return EMPTY;
          })
        )
      )
    )
  );

  const disconnectAccount = rxMethod<string>(
    pipe(
      switchMap(accountId =>
        bankSyncService.disconnectAccount(accountId).pipe(
          tap(() => load()),
          catchError((err: unknown) => {
            store.setError(extractErrorCode(err));
            return EMPTY;
          })
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
