import {type AccountIdentity} from '@shared/models/account-identity/account-identity.model';
import {type Timestamped} from '@shared/models/timestamped/timestamped.model';

export type SyncStatus = 'pending' | 'syncing' | 'active' | 'failed' | 'reauth_required';

export interface BankAccount extends AccountIdentity, Timestamped {
  ownerName: string;
  currentBalance: number;
  availableBalance: number;
  syncStatus: SyncStatus;
  lastSyncTimestamp: Nullable<string>;
  lastSyncDurationMs: Nullable<number>;
  provider: string;
}

export interface AccountsResponse {
  accounts: BankAccount[];
  totalCount: number;
  currency_totals: Record<string, number>;
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
