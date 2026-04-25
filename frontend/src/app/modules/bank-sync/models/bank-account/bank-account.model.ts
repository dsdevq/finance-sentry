export type SyncStatus = 'pending' | 'syncing' | 'active' | 'failed' | 'reauth_required';

export type Provider = 'plaid' | 'monobank';

export interface BankAccount {
  accountId: string;
  bankName: string;
  accountType: string;
  accountNumberLast4: string;
  ownerName: string;
  currency: string;
  currentBalance: number;
  availableBalance: number;
  syncStatus: SyncStatus;
  lastSyncTimestamp: Nullable<string>;
  lastSyncDurationMs: Nullable<number>;
  createdAt: string;
  provider: string;
}

export interface AccountsResponse {
  accounts: BankAccount[];
  totalCount: number;
  currency_totals: Record<string, number>;
}

export interface ConnectResponse {
  linkToken: string;
  expiresIn: number;
  expiresAt: string;
  requestId: string;
}

export interface ConnectedMonobankAccount {
  id: string;
  bankName: string;
  accountType: string;
  accountNumberLast4: string;
  ownerName: string;
  currency: string;
  currentBalance: Nullable<number>;
  syncStatus: string;
  provider: string;
}

export interface ConnectMonobankResponse {
  accounts: ConnectedMonobankAccount[];
}

export interface LinkAccountResponse {
  accountId: string;
  bankName: string;
  accountType: string;
  accountNumberLast4: string;
  ownerName: string;
  currency: string;
  initialBalance: number;
  syncStatus: string;
  lastSyncTimestamp: Nullable<string>;
  message: string;
}
