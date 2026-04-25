import {type WealthSummaryResponse} from '../../../../shared/models/wealth/wealth.model';

export interface AccountsState {
  summary: Nullable<WealthSummaryResponse>;
  status: AsyncStatus;
  errorCode: Nullable<string>;
}

export const initialAccountsState: AccountsState = {
  summary: null,
  status: 'idle',
  errorCode: null,
};
