import {inject, untracked} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, pipe, switchMap, tap, timer} from 'rxjs';

import {ErrorUtils} from '../../../../shared/utils/error.utils';
import {StoreErrorUtils} from '../../../../shared/utils/store-error.utils';
import {type DashboardData} from '../../models/dashboard/dashboard.model';
import {BankSyncService} from '../../services/bank-sync.service';

interface EffectsStore {
  setLoading: () => void;
  setData: (data: DashboardData) => void;
  setError: (errorCode: Nullable<string>) => void;
}

const MINUTES_PER_REFRESH = 5;
const SECONDS_PER_MINUTE = 60;
const MS_PER_SECOND = 1000;
const REFRESH_INTERVAL_MS = MINUTES_PER_REFRESH * SECONDS_PER_MINUTE * MS_PER_SECOND;

export function dashboardEffects(store: EffectsStore) {
  const bankSyncService = inject(BankSyncService);

  return {
    load: rxMethod<void>(
      pipe(
        tap(() => store.setLoading()),
        switchMap(() =>
          bankSyncService.getDashboardData().pipe(
            tap(data => store.setData(data)),
            StoreErrorUtils.catchAndSetError(store)
          )
        )
      )
    ),
  };
}

interface HookStore extends EffectsStore {
  load: () => void;
}

export function dashboardHooks(store: HookStore): void {
  const bankSyncService = inject(BankSyncService);

  store.load();

  rxMethod<void>(
    pipe(
      switchMap(() =>
        timer(REFRESH_INTERVAL_MS, REFRESH_INTERVAL_MS).pipe(
          switchMap(() => bankSyncService.getDashboardData()),
          tap(data => untracked(() => store.setData(data))),
          catchError((err: unknown) => {
            untracked(() => store.setError(ErrorUtils.extractCode(err)));
            return EMPTY;
          })
        )
      )
    )
  )();
}
