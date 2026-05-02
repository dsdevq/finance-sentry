import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type TransactionListResponse} from '../../models/transaction/transaction.model';
import {type TransactionsState} from './transactions.state';

export function transactionsMethods(store: WritableStateSource<TransactionsState>) {
  return {
    setAccountId(accountId: string): void {
      patchState(store, {accountId});
    },
    setDateRange(startDate: string, endDate: string): void {
      patchState(store, {startDate, endDate});
    },
    setResponse(res: TransactionListResponse): void {
      patchState(store, {
        transactions: res.items,
        bankName: res.bankName,
        currency: res.currency,
      });
    },
  };
}
