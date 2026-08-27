import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type MonthlyFlow} from '../../models/dashboard/dashboard.model';
import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';
import {type IncomeState} from './income.state';

export function incomeMethods(store: WritableStateSource<IncomeState>) {
  return {
    setMonthlyFlow(monthlyFlow: MonthlyFlow[]): void {
      patchState(store, {monthlyFlow});
    },
    setTransactions(
      transactions: GlobalTransactionDto[],
      totalCount: number,
      hasMore: boolean
    ): void {
      patchState(store, {transactions, totalCount, hasMore, offset: transactions.length});
    },
    appendTransactions(
      transactions: GlobalTransactionDto[],
      totalCount: number,
      hasMore: boolean
    ): void {
      patchState(store, s => ({
        transactions: [...s.transactions, ...transactions],
        totalCount,
        hasMore,
        offset: s.transactions.length + transactions.length,
      }));
    },
  };
}
