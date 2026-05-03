import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type Alert, type AlertFilter} from '../../models/alert/alert.model';
import {type AlertsState} from './alerts.state';

export function alertsMethods(store: WritableStateSource<AlertsState>) {
  return {
    setData(alerts: Alert[], totalCount: number, unreadCount: number): void {
      patchState(store, {alerts, totalCount, unreadCount, status: 'idle'});
    },
    setUnreadCount(unreadCount: number): void {
      patchState(store, {unreadCount});
    },
    setStatus(status: AsyncStatus): void {
      patchState(store, {status});
    },
    setFilter(filter: AlertFilter): void {
      patchState(store, {filter, currentPage: 1});
    },
    setPage(currentPage: number): void {
      patchState(store, {currentPage});
    },
    setPageSize(pageSize: number): void {
      patchState(store, {pageSize, currentPage: 1});
    },
    markReadLocal(id: string): void {
      patchState(store, (s: AlertsState) => ({
        alerts: s.alerts.map(a => (a.id === id ? {...a, isRead: true} : a)),
        unreadCount: Math.max(0, s.unreadCount - 1),
      }));
    },
    markAllReadLocal(): void {
      patchState(store, (s: AlertsState) => ({
        alerts: s.alerts.map(a => ({...a, isRead: true})),
        unreadCount: 0,
      }));
    },
    dismissLocal(id: string): void {
      patchState(store, (s: AlertsState) => ({
        alerts: s.alerts.filter(a => a.id !== id),
        totalCount: Math.max(0, s.totalCount - 1),
      }));
    },
  };
}
