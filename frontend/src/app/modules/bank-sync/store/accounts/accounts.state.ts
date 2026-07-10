import {type WealthSummaryResponse} from '../../../../shared/models/wealth/wealth.model';

export interface AccountsState {
  summary: Nullable<WealthSummaryResponse>;
  /** Bumped after every successful disconnect so sibling stores (e.g. HoldingsStore) can react. */
  disconnectVersion: number;
}

export const initialAccountsState: AccountsState = {
  summary: null,
  disconnectVersion: 0,
};
