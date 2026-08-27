import {inject, type Signal} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, forkJoin, of, pipe, switchMap, tap} from 'rxjs';

import {StoreErrorUtils} from '../../../../shared/utils/store-error.utils';
import {type MonthlyFlow} from '../../models/dashboard/dashboard.model';
import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {PAGE_SIZE} from './transaction-ledger.state';

const MONTH_KEY_PAD = 2;

function currentUtcMonthKey(): string {
  const now = new Date();
  return `${now.getUTCFullYear()}-${String(now.getUTCMonth() + 1).padStart(MONTH_KEY_PAD, '0')}`;
}

function sumCurrentMonthOutflow(monthlyFlow: MonthlyFlow[]): number {
  const key = currentUtcMonthKey();
  return monthlyFlow.filter(r => r.month === key).reduce((sum, r) => sum + r.outflowUsd, 0);
}

interface EffectsStore {
  offset: Signal<number>;
  setLoading: () => void;
  setTransactions: (
    transactions: GlobalTransactionDto[],
    totalCount: number,
    hasMore: boolean
  ) => void;
  appendTransactions: (
    transactions: GlobalTransactionDto[],
    totalCount: number,
    hasMore: boolean
  ) => void;
  nextPage: () => void;
  setError: (errorCode: Nullable<string>) => void;
  setMonthlyOutflowUsd: (value: number | null) => void;
}

export function transactionLedgerEffects(store: EffectsStore) {
  const bankSyncService = inject(BankSyncService);

  return {
    load: rxMethod<void>(
      pipe(
        tap(() => store.setLoading()),
        switchMap(() =>
          forkJoin({
            txResponse: bankSyncService.getAllTransactions({offset: 0, limit: PAGE_SIZE}),
            dashboardData: bankSyncService.getDashboardData().pipe(catchError(() => of(null))),
          }).pipe(
            tap(({txResponse, dashboardData}) => {
              store.setTransactions(txResponse.items, txResponse.totalCount, txResponse.hasMore);
              store.setMonthlyOutflowUsd(
                dashboardData ? sumCurrentMonthOutflow(dashboardData.monthlyFlow) : null
              );
            }),
            StoreErrorUtils.catchAndSetError(store)
          )
        )
      )
    ),
    loadMore: rxMethod<void>(
      pipe(
        tap(() => {
          store.nextPage();
          store.setLoading();
        }),
        switchMap(() =>
          bankSyncService.getAllTransactions({offset: store.offset(), limit: PAGE_SIZE}).pipe(
            tap(res => store.appendTransactions(res.items, res.totalCount, res.hasMore)),
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
