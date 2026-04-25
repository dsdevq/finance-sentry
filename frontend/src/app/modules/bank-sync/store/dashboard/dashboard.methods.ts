import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type DashboardData} from '../../models/dashboard/dashboard.model';
import {type DashboardState} from './dashboard.state';

export function dashboardMethods(store: WritableStateSource<DashboardState>) {
  return {
    setLoading(): void {
      patchState(store, {status: 'loading', errorCode: null});
    },
    setData(data: DashboardData): void {
      patchState(store, {data, status: 'idle', errorCode: null});
    },
    setError(errorCode: Nullable<string>): void {
      patchState(store, {status: 'error', errorCode});
    },
  };
}
