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
  institutionCount: number;
  accounts: AccountBalanceItem[];
}

/**
 * Client-side grouping of accounts under one institution
 * (Monobank cards, Binance assets, IBKR positions, …).
 * Produced by `InstitutionUtils.groupByInstitution` from a flat
 * {@link AccountBalanceItem} list — the backend still returns per-row data.
 */
export interface InstitutionGroup {
  key: string;
  name: string;
  provider: string;
  category: AccountCategory;
  accounts: AccountBalanceItem[];
  totalInBaseCurrency: number;
  worstSyncStatus: SyncStatus;
  latestSyncTimestamp: Nullable<string>;
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
