import {type DashboardData} from '../../models/dashboard/dashboard.model';

export interface DashboardState {
  data: Nullable<DashboardData>;
  status: AsyncStatus;
  errorCode: Nullable<string>;
}

export const initialDashboardState: DashboardState = {
  data: null,
  status: 'loading',
  errorCode: null,
};
