export type HistoryRange = '3m' | '6m' | '1y' | 'all';

export interface NetWorthSnapshotDto {
  snapshotDate: string;
  bankingTotal: number;
  brokerageTotal: number;
  cryptoTotal: number;
  totalNetWorth: number;
  currency: string;
}

export interface NetWorthHistoryResponse {
  snapshots: NetWorthSnapshotDto[];
  hasHistory: boolean;
}

export interface MonthlyFlow {
  month: string;
  currency: string;
  inflow: number;
  outflow: number;
  net: number;
}

export interface CategoryStat {
  category: string;
  totalSpend: number;
  percentOfTotal: number;
}

export interface DashboardData {
  aggregatedBalance: Record<string, number>;
  totalNetWorthUsd: number;
  accountCount: number;
  accountsByType: Record<string, number>;
  monthlyFlow: MonthlyFlow[];
  topCategories: CategoryStat[];
  lastSyncTimestamp: Nullable<string>;
}
