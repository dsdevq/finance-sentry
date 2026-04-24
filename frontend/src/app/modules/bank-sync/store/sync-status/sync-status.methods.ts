import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type SyncStatusResponse} from '../../models/sync.model';
import {type SyncStatusState} from './sync-status.state';

const FALLBACK_SYNC_FAILED = 'Sync failed. Please try again.';
const FALLBACK_TRIGGER_FAILED = 'Failed to trigger sync. Please try again.';
const FALLBACK_POLL_FAILED = 'Sync status check failed.';

export function syncStatusMethods(store: WritableStateSource<SyncStatusState>) {
  return {
    setAccountId(accountId: string): void {
      patchState(store, {accountId});
    },
    setStatus(status: SyncStatusResponse): void {
      const isSyncing = status.status === 'running' || status.status === 'pending';
      const errorMessage =
        status.status === 'failed' ? (status.errorMessage ?? FALLBACK_SYNC_FAILED) : null;
      patchState(store, {status, isSyncing, errorMessage});
    },
    markTriggering(): void {
      patchState(store, {isSyncing: true, errorMessage: null});
    },
    markTriggerFailed(): void {
      patchState(store, {isSyncing: false, errorMessage: FALLBACK_TRIGGER_FAILED});
    },
    markPollFailed(): void {
      patchState(store, {isSyncing: false, errorMessage: FALLBACK_POLL_FAILED});
    },
  };
}
