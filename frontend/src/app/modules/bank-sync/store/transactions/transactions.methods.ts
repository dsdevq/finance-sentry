import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type TransactionListResponse} from '../../models/transaction/transaction.model';
import {PAGE_SIZE, type TransactionsState} from './transactions.state';

export function transactionsMethods(store: WritableStateSource<TransactionsState>) {
  return {
    setAccountId(accountId: string): void {
      patchState(store, {accountId, offset: 0});
    },
    setDateRange(startDate: string, endDate: string): void {
      patchState(store, {startDate, endDate, offset: 0});
    },
    setOffset(offset: number): void {
      patchState(store, {offset});
    },
    setLoading(): void {
      patchState(store, {status: 'loading', errorCode: null});
    },
    setResponse(res: TransactionListResponse): void {
      patchState(store, {
        transactions: res.transactions,
        totalCount: res.pagination.totalCount,
        bankName: res.bankName,
        currency: res.currency,
        status: 'idle',
        errorCode: null,
      });
    },
    setError(errorCode: Nullable<string>): void {
      patchState(store, {status: 'error', errorCode});
    },
    nextPage(): void {
      patchState(store, state => ({offset: state.offset + PAGE_SIZE}));
    },
    previousPage(): void {
      patchState(store, state => ({offset: Math.max(0, state.offset - PAGE_SIZE)}));
    },
  };
}
