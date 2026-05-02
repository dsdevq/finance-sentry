export interface NetWorthPoint {
  month: string;
  banking: number;
  brokerage: number;
  crypto: number;
  total: number;
}

export const NET_WORTH_HISTORY_MOCK: NetWorthPoint[] = [
  {month: 'Apr 25', banking: 18400, brokerage: 95200, crypto: 9800, total: 123400},
  {month: 'May 25', banking: 19100, brokerage: 97600, crypto: 11200, total: 127900},
  {month: 'Jun 25', banking: 17800, brokerage: 99400, crypto: 8400, total: 125600},
  {month: 'Jul 25', banking: 20300, brokerage: 103200, crypto: 12600, total: 136100},
  {month: 'Aug 25', banking: 21200, brokerage: 106800, crypto: 14100, total: 142100},
  {month: 'Sep 25', banking: 19600, brokerage: 104100, crypto: 10200, total: 133900},
  {month: 'Oct 25', banking: 22100, brokerage: 108400, crypto: 15800, total: 146300},
  {month: 'Nov 25', banking: 21800, brokerage: 111600, crypto: 17200, total: 150600},
  {month: 'Dec 25', banking: 23400, brokerage: 115200, crypto: 13900, total: 152500},
  {month: 'Jan 26', banking: 22700, brokerage: 118400, crypto: 16400, total: 157500},
  {month: 'Feb 26', banking: 21900, brokerage: 122100, crypto: 14700, total: 158700},
  {month: 'Mar 26', banking: 22600, brokerage: 124800, crypto: 17300, total: 164700},
  {month: 'Apr 26', banking: 21800, brokerage: 126420, crypto: 19040, total: 167260},
];

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
