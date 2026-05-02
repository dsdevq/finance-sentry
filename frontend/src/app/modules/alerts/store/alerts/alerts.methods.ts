import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type Alert, type AlertFilter} from '../../models/alert/alert.model';
import {type AlertsState} from './alerts.state';

export function alertsMethods(store: WritableStateSource<AlertsState>) {
  return {
    setData(alerts: Alert[]): void {
      patchState(store, {alerts, status: 'idle'});
    },
    setFilter(filter: AlertFilter): void {
      patchState(store, {filter});
    },
    markRead(id: string): void {
      patchState(store, (s: AlertsState) => ({
        alerts: s.alerts.map(a => (a.id === id ? {...a, read: true} : a)),
      }));
    },
    markAllRead(): void {
      patchState(store, (s: AlertsState) => ({
        alerts: s.alerts.map(a => ({...a, read: true})),
      }));
    },
    dismiss(id: string): void {
      patchState(store, (s: AlertsState) => ({
        alerts: s.alerts.filter(a => a.id !== id),
      }));
    },
    clearAll(): void {
      patchState(store, {alerts: []});
    },
  };
}
