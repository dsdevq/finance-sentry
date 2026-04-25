export type AccountCategory = 'banking' | 'crypto' | 'brokerage' | 'other';

export type SyncStatus = 'synced' | 'syncing' | 'pending' | 'stale' | 'failed' | 'reauth_required';

export interface AccountBalanceItem {
  accountId: string;
  bankName: string;
  accountType: string;
  accountNumberLast4: string;
  provider: string;
  category: AccountCategory;
  currency: string;
  currentBalance: number;
  balanceInBaseCurrency: Nullable<number>;
  syncStatus: SyncStatus;
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
