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

/**
 * An institution the user has connected: one Monobank customer, one TrueLayer
 * connection (Revolut, AIB), one Binance account, one IBKR account.
 * Composed by the backend so the frontend never has to guess grouping.
 * Disconnect operates at this level and cascades to every account inside.
 */
export interface Institution {
  institutionId: string;
  provider: string;
  name: string;
  category: AccountCategory;
  totalInBaseCurrency: number;
  syncStatus: SyncStatus;
  lastSyncTimestamp: Nullable<string>;
  accounts: AccountBalanceItem[];
}

export interface CategorySummary {
  category: AccountCategory;
  totalInBaseCurrency: number;
  institutionCount: number;
  institutions: Institution[];
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
