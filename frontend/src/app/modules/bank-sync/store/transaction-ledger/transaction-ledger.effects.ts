import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, pipe, switchMap, tap} from 'rxjs';

import {type GlobalTransactionDto} from '../../models/transaction.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {PAGE_SIZE} from './transaction-ledger.state';

interface EffectsStore {
  setLoading: () => void;
  setTransactions: (
    transactions: GlobalTransactionDto[],
    totalCount: number,
    hasMore: boolean
  ) => void;
  setError: (errorCode: string | null) => void;
}

function extractErrorCode(err: unknown): string | null {
  const code = (err as {error?: {errorCode?: string}} | null)?.error?.errorCode;
  return code ?? null;
}

export function transactionLedgerEffects(store: EffectsStore) {
  const bankSyncService = inject(BankSyncService);

  return {
    load: rxMethod<void>(
      pipe(
        tap(() => store.setLoading()),
        switchMap(() =>
          bankSyncService.getAllTransactions({offset: 0, limit: PAGE_SIZE}).pipe(
            tap(res => store.setTransactions(res.transactions, res.totalCount, res.hasMore)),
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

export function transactionLedgerHooks(store: HookStore): void {
  store.load();
}
