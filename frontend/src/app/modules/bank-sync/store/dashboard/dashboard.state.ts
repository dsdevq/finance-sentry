import {
  type DashboardData,
  type HistoryRange,
  type NetWorthSnapshotDto,
} from '../../models/dashboard/dashboard.model';

export interface DashboardState {
  data: Nullable<DashboardData>;
  netWorthHistory: NetWorthSnapshotDto[];
  historyRange: HistoryRange;
  historyHasHistory: boolean;
  historyLoading: boolean;
  historyError: string | null;
  netWorthStacked: boolean;
}

export const initialDashboardState: DashboardState = {
  data: null,
  netWorthHistory: [],
  historyRange: '3m',
  historyHasHistory: false,
  historyLoading: false,
  historyError: null,
  netWorthStacked: true,
};
