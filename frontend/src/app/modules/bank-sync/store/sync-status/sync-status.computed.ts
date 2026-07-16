import {computed, type Signal} from '@angular/core';

import {type SyncStatusResponse} from '../../models/sync/sync.model';

interface StateSignals {
  status: Signal<Nullable<SyncStatusResponse>>;
  isSyncing: Signal<boolean>;
}

export function syncStatusComputed(store: StateSignals) {
  return {
    badgeClass: computed(() => {
      const status = store.status();

      return {
        'badge-syncing': store.isSyncing(),
        'badge-success': status?.status === 'success',
        'badge-failed': status?.status === 'failed',
        'badge-pending': !status,
      };
    }),
  };
}
