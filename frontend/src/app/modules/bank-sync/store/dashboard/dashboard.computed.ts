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

const SLEEVE_COLOR = {banking: '#10b981', brokerage: '#6366f1', crypto: '#f59e0b'} as const;
const SPENDING_COLOR = '#6366f1';

// Need at least a start and end snapshot to state a change over the window.
const MIN_POINTS_FOR_DELTA = 2;

function currentMonthKey(): string {
  const now = new Date();
  const MONTH_KEY_PAD = 2;
  return `${now.getUTCFullYear()}-${String(now.getUTCMonth() + 1).padStart(MONTH_KEY_PAD, '0')}`;
}

function formatMonthKey(key: string): string {
  const [year, month] = key.split('-').map(Number);
  return MONTH_FORMATTER.format(new Date(Date.UTC(year, month - 1, 1)));
}

/** Collapse the per-currency monthly rows into one USD outflow per month, sorted chronologically. */
function groupMonthlyOutflow(rows: MonthlyFlow[]): [string, number][] {
  const byMonth = new Map<string, number>();
  for (const r of rows) {
    byMonth.set(r.month, (byMonth.get(r.month) ?? 0) + r.outflowUsd);
  }
  return [...byMonth.entries()].sort(([a], [b]) => a.localeCompare(b));
}

function currentMonthOutflow(rows: MonthlyFlow[]): Nullable<number> {
  const key = currentMonthKey();
  const forMonth = rows.filter(r => r.month === key);
  if (forMonth.length === 0) {
    return null;
  }
  return forMonth.reduce((sum, r) => sum + r.outflowUsd, 0);
}

export function dashboardComputed(store: StateSignals) {
  const currency = inject(CurrencyPipe);
  const categoryStore = inject(CategoryStore);

  // Snapshots with a real total; days with a missing feed land as 0 and would otherwise
  // render as a cliff down to the axis, reading as if net worth briefly vanished.
  const validHistory = computed(() => store.netWorthHistory().filter(s => s.totalNetWorth > 0));

  const snapshotLabeller = computed((): ((s: NetWorthSnapshotDto) => string) => {
    const history = validHistory();
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

    // Signed net-worth change across the loaded window.
    netWorthChangeFormatted: computed(() => {
      const history = validHistory();
      if (history.length < MIN_POINTS_FOR_DELTA) {
        return '—';
      }
      const delta = history[history.length - 1].totalNetWorth - history[0].totalNetWorth;
      const sign = delta >= 0 ? '+' : '−';
      return `${sign}${COMPACT_FORMATTER.format(Math.abs(delta))}`;
    }),

    monthlySpendingFormatted: computed(() => {
      const spend = currentMonthOutflow(store.data()?.monthlyFlow ?? []);
      return spend != null ? COMPACT_FORMATTER.format(spend) : '—';
    }),

    topCategoryLabel: computed(() => {
      const top = store.data()?.topCategories?.[0];
      if (!top) {
        return 'Top Category';
      }
      return categoryStore.labelMap()[top.category] ?? MerchantCategoryUtils.format(top.category);
    }),

    topCategoryFormatted: computed(() => {
      const top = store.data()?.topCategories?.[0];
      return top ? (currency.transform(top.totalSpend) ?? '—') : '—';
    }),

    // Stacked net-worth composition (banking / brokerage / crypto) over time — the snapshots
    // already carry each sleeve, so we plot the mix rather than throwing it away for one line.
    netWorthAreaSeries: computed((): AreaSeries[] => {
      const history = validHistory();
      if (history.length === 0) {
        return [];
      }
      const labelOf = snapshotLabeller();
      const labels = history.map(labelOf);
      // A sleeve reading 0 on a given day means its feed was missing, not that the
      // balance vanished — carry the last-known value forward so the stack doesn't
      // collapse to the axis and read as a crash.
      const carryForward = (pick: (s: NetWorthSnapshotDto) => number): number[] => {
        let last = 0;
        return history.map(s => {
          const value = pick(s);
          if (value > 0) {
            last = value;
          }
          return last;
        });
      };
      const toPoints = (values: number[]) => values.map((value, i) => ({label: labels[i], value}));
      return [
        {
          label: 'Banking',
          color: SLEEVE_COLOR.banking,
          points: toPoints(carryForward(s => s.bankingTotal)),
        },
        {
          label: 'Brokerage',
          color: SLEEVE_COLOR.brokerage,
          points: toPoints(carryForward(s => s.brokerageTotal)),
        },
        {
          label: 'Crypto',
          color: SLEEVE_COLOR.crypto,
          points: toPoints(carryForward(s => s.cryptoTotal)),
        },
      ];
    }),

    monthlySpendingBars: computed((): BarSeries[] => {
      const grouped = groupMonthlyOutflow(store.data()?.monthlyFlow ?? []);
      if (grouped.length === 0) {
        return [];
      }
      return [
        {
          label: 'Spending',
          color: SPENDING_COLOR,
          points: grouped.map(([key, value]) => ({label: formatMonthKey(key), value})),
        },
      ];
    }),

    hasSpending: computed(() => (store.data()?.monthlyFlow ?? []).length > 0),

    categoryChartData: computed((): DonutSegment[] =>
      (store.data()?.topCategories ?? []).map(c => ({
        label: categoryStore.labelMap()[c.category] ?? MerchantCategoryUtils.format(c.category),
        value: c.totalSpend,
      }))
    ),

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
