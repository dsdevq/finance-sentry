import {
  type AccountBalanceItem,
  type InstitutionGroup,
  type SyncStatus,
} from '../models/wealth/wealth.model';

const SYNC_STATUS_PRIORITY: Record<SyncStatus, number> = {
  // eslint-disable-next-line @typescript-eslint/naming-convention
  reauth_required: 0,
  failed: 1,
  stale: 2,
  pending: 3,
  syncing: 4,
  synced: 5,
};

export class InstitutionUtils {
  /**
   * Group a flat list of account rows into institution-level groups.
   * Grouping key is `(provider, bankName)` — Monobank cards collapse under
   * their bank, Binance assets under "Binance", IBKR positions under "IBKR".
   */
  public static groupByInstitution(accounts: AccountBalanceItem[]): InstitutionGroup[] {
    const buckets = new Map<string, InstitutionGroup>();

    for (const account of accounts) {
      const key = `${account.provider}::${account.bankName}`;
      const existing = buckets.get(key);
      if (existing) {
        existing.accounts.push(account);
        existing.totalInBaseCurrency += account.balanceInBaseCurrency ?? 0;
        existing.worstSyncStatus = InstitutionUtils.worseStatus(
          existing.worstSyncStatus,
          account.syncStatus
        );
        existing.latestSyncTimestamp = InstitutionUtils.laterTimestamp(
          existing.latestSyncTimestamp,
          account.lastSyncTimestamp
        );
      } else {
        buckets.set(key, {
          key,
          name: account.bankName,
          provider: account.provider,
          category: account.category,
          accounts: [account],
          totalInBaseCurrency: account.balanceInBaseCurrency ?? 0,
          worstSyncStatus: account.syncStatus,
          latestSyncTimestamp: account.lastSyncTimestamp,
        });
      }
    }

    return [...buckets.values()].sort((a, b) => b.totalInBaseCurrency - a.totalInBaseCurrency);
  }

  private static worseStatus(a: SyncStatus, b: SyncStatus): SyncStatus {
    return SYNC_STATUS_PRIORITY[a] <= SYNC_STATUS_PRIORITY[b] ? a : b;
  }

  private static laterTimestamp(a: Nullable<string>, b: Nullable<string>): Nullable<string> {
    if (a === null) {
      return b;
    }
    if (b === null) {
      return a;
    }
    return a > b ? a : b;
  }
}
