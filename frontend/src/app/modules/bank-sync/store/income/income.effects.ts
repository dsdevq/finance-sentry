import {inject, type Signal} from '@angular/core';
import {extractErrorCode} from '@dsdevq-common/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, forkJoin, pipe, switchMap, tap} from 'rxjs';

import {type MonthlyFlow} from '../../models/dashboard/dashboard.model';
import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {INCOME_PAGE_SIZE} from './income.state';

interface EffectsStore {
  offset: Signal<number>;
  setLoading: () => void;
  setSuccess: () => void;
  setError: (errorCode: Nullable<string>) => void;
  setMonthlyFlow: (flow: MonthlyFlow[]) => void;
  setTransactions: (txns: GlobalTransactionDto[], totalCount: number, hasMore: boolean) => void;
  appendTransactions: (txns: GlobalTransactionDto[], totalCount: number, hasMore: boolean) => void;
}

export function incomeEffects(store: EffectsStore) {
  const bankSyncService = inject(BankSyncService);

  return {
    load: rxMethod<void>(
      pipe(
        tap(() => store.setLoading()),
        switchMap(() =>
          forkJoin({
            dashboard$: bankSyncService.getDashboardData(),
            txns$: bankSyncService.getAllTransactions({
              offset: 0,
              limit: INCOME_PAGE_SIZE,
              transactionType: 'credit',
            }),
          }).pipe(
            tap(({dashboard$, txns$}) => {
              store.setMonthlyFlow(dashboard$.monthlyFlow);
              store.setTransactions(txns$.items, txns$.totalCount, txns$.hasMore);
              store.setSuccess();
            }),
            catchError((err: unknown) => {
              store.setError(extractErrorCode(err));
              return EMPTY;
            })
          )
        )
      )
    ),

    loadMore: rxMethod<void>(
      pipe(
        tap(() => store.setLoading()),
        switchMap(() =>
          bankSyncService
            .getAllTransactions({
              offset: store.offset(),
              limit: INCOME_PAGE_SIZE,
              transactionType: 'credit',
            })
            .pipe(
              tap(res => {
                store.appendTransactions(res.items, res.totalCount, res.hasMore);
                store.setSuccess();
              }),
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

export function incomeHooks(store: HookStore): void {
  store.load();
}
