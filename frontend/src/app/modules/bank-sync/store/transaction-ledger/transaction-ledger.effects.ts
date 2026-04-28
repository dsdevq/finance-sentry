import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {pipe, switchMap, tap} from 'rxjs';

import {StoreErrorUtils} from '../../../../shared/utils/store-error.utils';
import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {PAGE_SIZE} from './transaction-ledger.state';

interface EffectsStore {
  setLoading: () => void;
  setTransactions: (
    transactions: GlobalTransactionDto[],
    totalCount: number,
    hasMore: boolean
  ) => void;
  setError: (errorCode: Nullable<string>) => void;
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
            StoreErrorUtils.catchAndSetError(store)
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
