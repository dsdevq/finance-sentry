import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type BankAccount} from '../../models/bank-account.model';
import {type AccountsState} from './accounts.state';

export function accountsMethods(store: WritableStateSource<AccountsState>) {
  return {
    setLoading(): void {
      patchState(store, {status: 'loading', errorCode: null});
    },
    setAccounts(accounts: BankAccount[]): void {
      patchState(store, {accounts, status: 'idle', errorCode: null});
    },
    setError(errorCode: string | null): void {
      patchState(store, {status: 'error', errorCode});
    },
    removeAccount(accountId: string): void {
      patchState(store, state => ({
        accounts: state.accounts.filter(a => a.accountId !== accountId),
      }));
    },
  };
}
