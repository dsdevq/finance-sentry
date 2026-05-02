import {inject, type Signal} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {extractErrorCode} from '@dsdevq-common/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, pipe, switchMap, tap} from 'rxjs';

import {type TransactionListResponse} from '../../models/transaction/transaction.model';
import {BankSyncService} from '../../services/bank-sync.service';

interface EffectsStore {
  accountId: Signal<string>;
  startDate: Signal<string>;
  endDate: Signal<string>;
  offset: Signal<number>;
  limit: Signal<number>;
  setLoading: () => void;
  setSuccess: () => void;
  setError: (code: Nullable<string>) => void;
  setAccountId: (id: string) => void;
  setResponse: (res: TransactionListResponse) => void;
  setTotalCount: (count: number) => void;
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
              limit: store.limit(),
              startDate: store.startDate() || undefined,
              endDate: store.endDate() || undefined,
            })
            .pipe(
              tap(res => {
                store.setResponse(res);
                store.setTotalCount(res.totalCount);
                store.setSuccess();
              }),
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

export function transactionsHooks(store: ReturnType<typeof transactionsEffects> & EffectsStore) {
  const route = inject(ActivatedRoute);

  return {
    onInit: () => {
      const accountId = route.snapshot.paramMap.get('accountId') ?? '';
      store.setAccountId(accountId);
      store.load();
    },
  };
}
