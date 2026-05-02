import {inject, untracked} from '@angular/core';
import {extractErrorCode} from '@dsdevq-common/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, pipe, switchMap, tap, timer} from 'rxjs';

import {type DashboardData} from '../../models/dashboard/dashboard.model';
import {BankSyncService} from '../../services/bank-sync.service';

interface EffectsStore {
  setLoading: () => void;
  setSuccess: () => void;
  setError: (errorCode: Nullable<string>) => void;
  setData: (data: DashboardData) => void;
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
            tap(data => {
              store.setData(data);
              store.setSuccess();
            }),
            catchError((err: unknown) => {
              store.setError(extractErrorCode(err));
              return EMPTY;
            })
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
            untracked(() => store.setError(extractErrorCode(err)));
            return EMPTY;
          })
        )
      )
    )
  )();
}
