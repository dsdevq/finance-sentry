import {type BankAccount} from '../../models/bank-account.model';

export type AccountsStatus = 'idle' | 'loading' | 'error';

export interface AccountsState {
  accounts: BankAccount[];
  status: AccountsStatus;
  errorCode: string | null;
}

export const initialAccountsState: AccountsState = {
  accounts: [],
  status: 'idle',
  errorCode: null,
};
