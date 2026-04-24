import {type DashboardData} from '../../models/dashboard.model';

export type DashboardStatus = 'idle' | 'loading' | 'error';

export interface DashboardState {
  data: DashboardData | null;
  status: DashboardStatus;
  errorCode: string | null;
}

export const initialDashboardState: DashboardState = {
  data: null,
  status: 'loading',
  errorCode: null,
};
