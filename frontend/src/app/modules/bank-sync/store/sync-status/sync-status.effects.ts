import {inject, type Signal} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, pipe, switchMap, tap} from 'rxjs';

import {type SyncStatusResponse} from '../../models/sync/sync.model';
import {BankSyncService} from '../../services/bank-sync.service';

interface EffectsStore {
  accountId: Signal<string>;
  setStatus: (status: SyncStatusResponse) => void;
  markTriggering: () => void;
  markTriggerFailed: () => void;
  markPollFailed: () => void;
}

export function syncStatusEffects(store: EffectsStore) {
  const bankSyncService = inject(BankSyncService);

  const pollStatus = rxMethod<void>(
    pipe(
      switchMap(() =>
        bankSyncService.pollSyncStatus(store.accountId()).pipe(
          tap(status => store.setStatus(status)),
          catchError(() => {
            store.markPollFailed();
            return EMPTY;
          })
        )
      )
    )
  );

  const loadStatus = rxMethod<void>(
    pipe(
      switchMap(() =>
        bankSyncService.getSyncStatus(store.accountId()).pipe(
          tap(status => store.setStatus(status)),
          catchError(() => EMPTY)
        )
      )
    )
  );

  const triggerSync = rxMethod<void>(
    pipe(
      tap(() => store.markTriggering()),
      switchMap(() =>
        bankSyncService.triggerSync(store.accountId()).pipe(
          tap(() => pollStatus()),
          catchError(() => {
            store.markTriggerFailed();
            return EMPTY;
          })
        )
      )
    )
  );

  return {loadStatus, triggerSync, pollStatus};
}
