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
  /** Annual market return assumed by the projection tile, as a fraction (0.05 = 5%/yr). */
  projectionReturnRate: number;
}

export const initialDashboardState: DashboardState = {
  data: null,
  netWorthHistory: [],
  historyRange: '3m',
  historyHasHistory: false,
  historyLoading: false,
  historyError: null,
  netWorthStacked: true,
  projectionReturnRate: 0,
};
