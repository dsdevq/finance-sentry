import {inject, type Signal} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {EMPTY, pipe, switchMap, tap} from 'rxjs';

import {StoreErrorUtils} from '../../../../shared/utils/store-error.utils';
import {type TransactionListResponse} from '../../models/transaction/transaction.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {PAGE_SIZE} from './transactions.state';

interface EffectsStore {
  accountId: Signal<string>;
  startDate: Signal<string>;
  endDate: Signal<string>;
  offset: Signal<number>;
  setLoading: () => void;
  setResponse: (res: TransactionListResponse) => void;
  setError: (code: Nullable<string>) => void;
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
              StoreErrorUtils.catchAndSetError(store)
            );
        })
      )
    ),
  };
}
