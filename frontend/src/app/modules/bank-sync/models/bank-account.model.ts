export type SyncStatus = 'pending' | 'syncing' | 'active' | 'failed' | 'reauth_required';

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
  lastSyncTimestamp: string | null;
  lastSyncDurationMs: number | null;
  createdAt: string;
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

export interface LinkAccountResponse {
  accountId: string;
  bankName: string;
  accountType: string;
  accountNumberLast4: string;
  ownerName: string;
  currency: string;
  initialBalance: number;
  syncStatus: string;
  lastSyncTimestamp: string | null;
  message: string;
}
