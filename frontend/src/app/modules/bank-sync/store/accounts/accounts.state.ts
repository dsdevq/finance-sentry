import {type WealthSummaryResponse} from '../../models/wealth.model';

export type AccountsStatus = 'idle' | 'loading' | 'error';

export interface AccountsState {
  summary: WealthSummaryResponse | null;
  status: AccountsStatus;
  errorCode: string | null;
}

export const initialAccountsState: AccountsState = {
  summary: null,
  status: 'idle',
  errorCode: null,
};
