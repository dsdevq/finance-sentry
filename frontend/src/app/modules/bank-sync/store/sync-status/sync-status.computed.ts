import {computed, type Signal} from '@angular/core';

import {type SyncStatusResponse} from '../../models/sync.model';

interface StateSignals {
  status: Signal<SyncStatusResponse | null>;
  isSyncing: Signal<boolean>;
}

export function syncStatusComputed(store: StateSignals) {
  return {
    badgeClass: computed(() => {
      const status = store.status();
      /* eslint-disable @typescript-eslint/naming-convention */
      return {
        'badge-syncing': store.isSyncing(),
        'badge-success': status?.status === 'success',
        'badge-failed': status?.status === 'failed',
        'badge-pending': !status,
      };
      /* eslint-enable @typescript-eslint/naming-convention */
    }),
  };
}
