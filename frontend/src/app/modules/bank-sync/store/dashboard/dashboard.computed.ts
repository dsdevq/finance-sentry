import {CurrencyPipe} from '@angular/common';
import {computed, inject, type Signal} from '@angular/core';
import {type AreaSeries, type BarSeries, type DonutSegment} from '@dsdevq-common/ui';

import {CategoryStore} from '../../../../shared/store/categories/categories.store';
import {MerchantCategoryUtils} from '../../../../shared/utils/merchant-category.utils';
import {
  type DashboardData,
  type MonthlyFlow,
  type NetWorthSnapshotDto,
} from '../../models/dashboard/dashboard.model';

interface StateSignals {
  data: Signal<Nullable<DashboardData>>;
  netWorthHistory: Signal<NetWorthSnapshotDto[]>;
  historyLoading: Signal<boolean>;
  historyError: Signal<string | null>;
}

const COMPACT_FORMATTER = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  notation: 'compact',
  minimumFractionDigits: 0,
  maximumFractionDigits: 1,
});

const MONTH_FORMATTER = new Intl.DateTimeFormat('en-US', {month: 'short', year: '2-digit'});
const DAY_FORMATTER = new Intl.DateTimeFormat('en-US', {month: 'short', day: 'numeric'});

// Below this span the snapshots are effectively daily, so month-year labels ("Jul 26")
// collapse to a single repeated value — use day-level labels ("Jul 5") instead.
const SHORT_SPAN_DAYS = 92;
const MS_PER_DAY = 86_400_000;

const MONTH_KEY_PAD = 2;
const PERCENT = 100;

const SLEEVE_COLOR = {banking: '#10b981', brokerage: '#6366f1', crypto: '#f59e0b'} as const;
const INCOME_COLOR = '#10b981';
const SPENDING_COLOR = '#ef4444';
const SAVINGS_COLOR = '#6366f1';

interface MonthTotals {
  inflow: number;
  outflow: number;
  net: number;
}

function currentMonthKey(): string {
  const now = new Date();
  return `${now.getUTCFullYear()}-${String(now.getUTCMonth() + 1).padStart(MONTH_KEY_PAD, '0')}`;
}

function formatMonthKey(key: string): string {
  const [year, month] = key.split('-').map(Number);
  return MONTH_FORMATTER.format(new Date(Date.UTC(year, month - 1, 1)));
}

/** Collapse the per-currency monthly rows into one USD total per month, sorted chronologically. */
function groupMonthlyFlow(rows: MonthlyFlow[]): [string, MonthTotals][] {
  const byMonth = new Map<string, MonthTotals>();
  for (const r of rows) {
    const cur = byMonth.get(r.month) ?? {inflow: 0, outflow: 0, net: 0};
    cur.inflow += r.inflowUsd;
    cur.outflow += r.outflowUsd;
    cur.net += r.netUsd;
    byMonth.set(r.month, cur);
  }
  return [...byMonth.entries()].sort(([a], [b]) => a.localeCompare(b));
}

function sumCurrentMonthUsd(rows: MonthlyFlow[]): Nullable<MonthTotals> {
  const key = currentMonthKey();
  const forMonth = rows.filter(r => r.month === key);
  if (forMonth.length === 0) {
    return null;
  }
  return forMonth.reduce<MonthTotals>(
    (acc, r) => ({
      inflow: acc.inflow + r.inflowUsd,
      outflow: acc.outflow + r.outflowUsd,
      net: acc.net + r.netUsd,
    }),
    {inflow: 0, outflow: 0, net: 0}
  );
}

export function dashboardComputed(store: StateSignals) {
  const currency = inject(CurrencyPipe);
  const categoryStore = inject(CategoryStore);

  const snapshotLabeller = computed((): ((s: NetWorthSnapshotDto) => string) => {
    const history = store.netWorthHistory();
    if (history.length === 0) {
      return s => s.snapshotDate;
    }
    const times = history.map(s => new Date(s.snapshotDate).getTime());
    const spanDays = (Math.max(...times) - Math.min(...times)) / MS_PER_DAY;
    const formatter = spanDays <= SHORT_SPAN_DAYS ? DAY_FORMATTER : MONTH_FORMATTER;
    return s => formatter.format(new Date(s.snapshotDate));
  });

  return {
    totalBalanceFormatted: computed(
      () => currency.transform(store.data()?.totalNetWorthUsd ?? 0) ?? ''
    ),

    latestInflowFormatted: computed(() => {
      const current = sumCurrentMonthUsd(store.data()?.monthlyFlow ?? []);
      return current ? COMPACT_FORMATTER.format(current.inflow) : '—';
    }),

    latestOutflowFormatted: computed(() => {
      const current = sumCurrentMonthUsd(store.data()?.monthlyFlow ?? []);
      return current ? COMPACT_FORMATTER.format(current.outflow) : '—';
    }),

    savingsRateFormatted: computed(() => {
      const current = sumCurrentMonthUsd(store.data()?.monthlyFlow ?? []);
      if (!current || current.inflow <= 0) {
        return '—';
      }
      return `${((current.net / current.inflow) * PERCENT).toFixed(0)}%`;
    }),

    // Stacked net-worth composition (banking / brokerage / crypto) over time — the snapshots
    // already carry each sleeve, so we plot the mix rather than throwing it away for one line.
    netWorthAreaSeries: computed((): AreaSeries[] => {
      const history = store.netWorthHistory();
      if (history.length === 0) {
        return [];
      }
      const labelOf = snapshotLabeller();
      return [
        {
          label: 'Banking',
          color: SLEEVE_COLOR.banking,
          points: history.map(s => ({label: labelOf(s), value: s.bankingTotal})),
        },
        {
          label: 'Brokerage',
          color: SLEEVE_COLOR.brokerage,
          points: history.map(s => ({label: labelOf(s), value: s.brokerageTotal})),
        },
        {
          label: 'Crypto',
          color: SLEEVE_COLOR.crypto,
          points: history.map(s => ({label: labelOf(s), value: s.cryptoTotal})),
        },
      ];
    }),

    cashFlowBars: computed((): BarSeries[] => {
      const grouped = groupMonthlyFlow(store.data()?.monthlyFlow ?? []);
      if (grouped.length === 0) {
        return [];
      }
      return [
        {
          label: 'Income',
          color: INCOME_COLOR,
          points: grouped.map(([key, v]) => ({label: formatMonthKey(key), value: v.inflow})),
        },
        {
          label: 'Spending',
          color: SPENDING_COLOR,
          points: grouped.map(([key, v]) => ({label: formatMonthKey(key), value: v.outflow})),
        },
      ];
    }),

    savingsRateBars: computed((): BarSeries[] => {
      const grouped = groupMonthlyFlow(store.data()?.monthlyFlow ?? []);
      if (grouped.length === 0) {
        return [];
      }
      return [
        {
          label: 'Savings rate',
          color: SAVINGS_COLOR,
          points: grouped.map(([key, v]) => ({
            label: formatMonthKey(key),
            value: v.inflow > 0 ? (v.net / v.inflow) * PERCENT : 0,
          })),
        },
      ];
    }),

    categoryChartData: computed((): DonutSegment[] =>
      (store.data()?.topCategories ?? []).map(c => ({
        label: categoryStore.labelMap()[c.category] ?? MerchantCategoryUtils.format(c.category),
        value: c.totalSpend,
      }))
    ),

    hasCashFlow: computed(() => (store.data()?.monthlyFlow ?? []).length > 0),

    netWorthStaleNotice: computed((): string | null => {
      const history = store.netWorthHistory();
      const latest = history[history.length - 1];
      const sleeves = latest?.staleSleeves?.trim();
      if (!sleeves) {
        return null;
      }
      const names = sleeves
        .split(',')
        .map(s => s.trim())
        .filter(Boolean)
        .map(s => s.charAt(0).toUpperCase() + s.slice(1));
      return `${names.join(' & ')} data is stale — carried forward from the last successful sync, not a real change.`;
    }),

    isHistoryLoading: computed(() => store.historyLoading()),
    historyErrorMessage: computed(() => store.historyError()),
  };
}
