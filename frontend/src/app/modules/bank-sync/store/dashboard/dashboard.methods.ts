import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type DashboardData} from '../../models/dashboard/dashboard.model';
import {type DashboardState} from './dashboard.state';

export function dashboardMethods(store: WritableStateSource<DashboardState>) {
  return {
    setData(data: DashboardData): void {
      patchState(store, {data});
    },
  };
}
