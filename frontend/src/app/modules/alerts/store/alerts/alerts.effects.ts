import {inject, type Signal} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, exhaustMap, pipe, switchMap, tap} from 'rxjs';

import {type Alert, type AlertFilter} from '../../models/alert/alert.model';
import {AlertsService} from '../../services/alerts.service';

interface EffectsStore {
  setData: (alerts: Alert[], totalCount: number, unreadCount: number) => void;
  setUnreadCount: (n: number) => void;
  setStatus: (status: AsyncStatus) => void;
  markReadLocal: (id: string) => void;
  markAllReadLocal: () => void;
  dismissLocal: (id: string) => void;
  filter: Signal<AlertFilter>;
  currentPage: Signal<number>;
  pageSize: Signal<number>;
}

export function alertsEffects(store: EffectsStore) {
  const api = inject(AlertsService);

  return {
    load: rxMethod<void>(
      pipe(
        tap(() => store.setStatus('loading')),
        switchMap(() =>
          api.getAlerts(store.filter(), store.currentPage(), store.pageSize()).pipe(
            tap(res => store.setData(res.items, res.totalCount, res.unreadCount)),
            catchError(() => {
              store.setStatus('error');
              return EMPTY;
            })
          )
        )
      )
    ),
    loadUnreadCount: rxMethod<void>(
      pipe(
        switchMap(() =>
          api.getUnreadCount().pipe(
            tap(res => store.setUnreadCount(res.count)),
            catchError(() => EMPTY)
          )
        )
      )
    ),
    markRead: rxMethod<string>(
      pipe(
        exhaustMap(id =>
          api.markRead(id).pipe(
            tap(() => store.markReadLocal(id)),
            catchError(() => EMPTY)
          )
        )
      )
    ),
    markAllRead: rxMethod<void>(
      pipe(
        exhaustMap(() =>
          api.markAllRead().pipe(
            tap(() => store.markAllReadLocal()),
            catchError(() => EMPTY)
          )
        )
      )
    ),
    dismiss: rxMethod<string>(
      pipe(
        exhaustMap(id =>
          api.dismiss(id).pipe(
            tap(() => store.dismissLocal(id)),
            catchError(() => EMPTY)
          )
        )
      )
    ),
  };
}

interface HookStore {
  load: () => void;
  loadUnreadCount: () => void;
}

export function alertsHooks(store: HookStore): void {
  store.load();
  store.loadUnreadCount();
}
