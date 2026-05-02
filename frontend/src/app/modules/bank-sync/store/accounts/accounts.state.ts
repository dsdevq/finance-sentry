import {type WealthSummaryResponse} from '../../../../shared/models/wealth/wealth.model';

export interface AccountsState {
  summary: Nullable<WealthSummaryResponse>;
}

export const initialAccountsState: AccountsState = {
  summary: null,
};
