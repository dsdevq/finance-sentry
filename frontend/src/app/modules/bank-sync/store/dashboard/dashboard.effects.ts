import {inject, type Signal, untracked} from '@angular/core';
import {toObservable} from '@angular/core/rxjs-interop';
import {extractErrorCode} from '@dsdevq-common/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, pipe, switchMap, tap, timer} from 'rxjs';

import {
  type DashboardData,
  type HistoryRange,
  type NetWorthSnapshotDto,
} from '../../models/dashboard/dashboard.model';
import {BankSyncService} from '../../services/bank-sync.service';

interface EffectsStore {
  setLoading: () => void;
  setSuccess: () => void;
  setError: (errorCode: Nullable<string>) => void;
  setData: (data: DashboardData) => void;
  setNetWorthHistory: (snapshots: NetWorthSnapshotDto[]) => void;
  setHistoryLoading: (loading: boolean) => void;
  setHistoryError: (error: string | null) => void;
  setHistoryHasHistory: (hasHistory: boolean) => void;
  historyRange: Signal<HistoryRange>;
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

    loadNetWorthHistory: rxMethod<HistoryRange>(
      pipe(
        tap(() => store.setHistoryLoading(true)),
        switchMap(range =>
          bankSyncService.getNetWorthHistory(range).pipe(
            tap(response => {
              store.setNetWorthHistory(response.snapshots);
              store.setHistoryHasHistory(response.hasHistory);
              store.setHistoryLoading(false);
              store.setHistoryError(null);
            }),
            catchError((err: unknown) => {
              store.setHistoryError(extractErrorCode(err) ?? 'Failed to load net worth history.');
              store.setHistoryLoading(false);
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
  loadNetWorthHistory: (range: HistoryRange) => void;
}

export function dashboardHooks(store: HookStore): void {
  const bankSyncService = inject(BankSyncService);

  store.load();
  store.loadNetWorthHistory(store.historyRange());

  toObservable(store.historyRange)
    .pipe(tap(range => untracked(() => store.loadNetWorthHistory(range))))
    .subscribe();

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
