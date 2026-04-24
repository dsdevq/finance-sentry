import {signalStore, withComputed, withMethods, withState} from '@ngrx/signals';

import {syncStatusComputed} from './sync-status.computed';
import {syncStatusEffects} from './sync-status.effects';
import {syncStatusMethods} from './sync-status.methods';
import {initialSyncStatusState} from './sync-status.state';

export const SyncStatusStore = signalStore(
  withState(initialSyncStatusState),
  withMethods(syncStatusMethods),
  withComputed(syncStatusComputed),
  withMethods(syncStatusEffects)
);
