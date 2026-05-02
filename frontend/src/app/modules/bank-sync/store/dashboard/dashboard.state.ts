import {type DashboardData} from '../../models/dashboard/dashboard.model';

export interface DashboardState {
  data: Nullable<DashboardData>;
}

export const initialDashboardState: DashboardState = {
  data: null,
};
