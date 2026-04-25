import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type GlobalTransactionDto} from '../../models/transaction.model';
import {type TransactionLedgerState} from './transaction-ledger.state';

export function transactionLedgerMethods(store: WritableStateSource<TransactionLedgerState>) {
  return {
    setLoading(): void {
      patchState(store, {status: 'loading', errorCode: null});
    },
    setTransactions(
      transactions: GlobalTransactionDto[],
      totalCount: number,
      hasMore: boolean
    ): void {
      patchState(store, {transactions, totalCount, hasMore, status: 'idle', errorCode: null});
    },
    setError(errorCode: string | null): void {
      patchState(store, {status: 'error', errorCode});
    },
    setOffset(offset: number): void {
      patchState(store, {offset});
    },
  };
}
