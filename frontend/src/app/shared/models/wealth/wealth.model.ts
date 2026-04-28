import {type AccountIdentity} from '../account-identity/account-identity.model';

export type AccountCategory = 'banking' | 'crypto' | 'brokerage' | 'other';

export type SyncStatus = 'synced' | 'syncing' | 'pending' | 'stale' | 'failed' | 'reauth_required';

export interface AccountBalanceItem extends AccountIdentity {
  provider: string;
  category: AccountCategory;
  currentBalance: number;
  balanceInBaseCurrency: Nullable<number>;
  syncStatus: SyncStatus;
  lastSyncTimestamp: Nullable<string>;
}

export interface CategorySummary {
  category: AccountCategory;
  totalInBaseCurrency: number;
  accounts: AccountBalanceItem[];
}

export interface AppliedFilters {
  category: Nullable<AccountCategory>;
  provider: Nullable<string>;
}

export interface WealthSummaryResponse {
  totalNetWorth: number;
  baseCurrency: string;
  categories: CategorySummary[];
  appliedFilters: AppliedFilters;
}
