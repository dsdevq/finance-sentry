import {inject, type Signal} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, pipe, switchMap, tap} from 'rxjs';

import {type TransactionListResponse} from '../../models/transaction.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {PAGE_SIZE} from './transactions.state';

interface EffectsStore {
  accountId: Signal<string>;
  startDate: Signal<string>;
  endDate: Signal<string>;
  offset: Signal<number>;
  setLoading: () => void;
  setResponse: (res: TransactionListResponse) => void;
  setError: (code: string | null) => void;
}

function extractErrorCode(err: unknown): string | null {
  const code = (err as {error?: {errorCode?: string}} | null)?.error?.errorCode;
  return code ?? null;
}

export function transactionsEffects(store: EffectsStore) {
  const bankSyncService = inject(BankSyncService);

  return {
    load: rxMethod<void>(
      pipe(
        tap(() => store.setLoading()),
        switchMap(() => {
          const accountId = store.accountId();
          if (!accountId) {
            return EMPTY;
          }
          return bankSyncService
            .getTransactions(accountId, {
              offset: store.offset(),
              limit: PAGE_SIZE,
              startDate: store.startDate() || undefined,
              endDate: store.endDate() || undefined,
              sort: 'date:desc',
            })
            .pipe(
              tap(res => store.setResponse(res)),
              catchError((err: unknown) => {
                store.setError(extractErrorCode(err));
                return EMPTY;
              })
            );
        })
      )
    ),
  };
}
