import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, pipe, switchMap, tap} from 'rxjs';

import {type BankAccount} from '../../models/bank-account.model';
import {BankSyncService} from '../../services/bank-sync.service';

interface EffectsStore {
  setLoading: () => void;
  setAccounts: (accounts: BankAccount[]) => void;
  setError: (errorCode: string | null) => void;
  removeAccount: (accountId: string) => void;
}

function extractErrorCode(err: unknown): string | null {
  const code = (err as {error?: {errorCode?: string}} | null)?.error?.errorCode;
  return code ?? null;
}

export function accountsEffects(store: EffectsStore) {
  const bankSyncService = inject(BankSyncService);

  return {
    load: rxMethod<void>(
      pipe(
        tap(() => store.setLoading()),
        switchMap(() =>
          bankSyncService.getAccounts().pipe(
            tap(res => store.setAccounts(res.accounts)),
            catchError((err: unknown) => {
              store.setError(extractErrorCode(err));
              return EMPTY;
            })
          )
        )
      )
    ),
    disconnect: rxMethod<string>(
      pipe(
        switchMap(accountId =>
          bankSyncService.disconnectAccount(accountId).pipe(
            tap(() => store.removeAccount(accountId)),
            catchError((err: unknown) => {
              store.setError(extractErrorCode(err));
              return EMPTY;
            })
          )
        )
      )
    ),
  };
}

interface HookStore {
  load: () => void;
}

export function accountsHooks(store: HookStore): void {
  store.load();
}
