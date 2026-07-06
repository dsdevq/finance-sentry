import {CurrencyPipe} from '@angular/common';
import {computed, inject, type Signal} from '@angular/core';
import {type ChartPoint, type DonutSegment} from '@dsdevq-common/ui';

import {CategoryStore} from '../../../../shared/store/categories/categories.store';
import {MerchantCategoryUtils} from '../../../../shared/utils/merchant-category.utils';
import {type DashboardData, type NetWorthSnapshotDto} from '../../models/dashboard/dashboard.model';

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

const MONTH_KEY_PAD = 2;

function currentMonthKey(): string {
  const now = new Date();
  return `${now.getUTCFullYear()}-${String(now.getUTCMonth() + 1).padStart(MONTH_KEY_PAD, '0')}`;
}

function sumCurrentMonthUsd(
  rows: {month: string; inflowUsd: number; outflowUsd: number}[]
): Nullable<{inflow: number; outflow: number}> {
  const key = currentMonthKey();
  const forMonth = rows.filter(r => r.month === key);
  if (forMonth.length === 0) {
    return null;
  }
  return {
    inflow: forMonth.reduce((s, r) => s + r.inflowUsd, 0),
    outflow: forMonth.reduce((s, r) => s + r.outflowUsd, 0),
  };
}

export function dashboardComputed(store: StateSignals) {
  const currency = inject(CurrencyPipe);
  const categoryStore = inject(CategoryStore);

  return {
    currencyEntries: computed(() => Object.entries(store.data()?.aggregatedBalance ?? {})),
    accountTypeEntries: computed(() => Object.entries(store.data()?.accountsByType ?? {})),

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

    netFlowChartData: computed((): ChartPoint[] => {
      const rows = store.data()?.monthlyFlow ?? [];
      const byMonth = new Map<string, number>();
      for (const m of rows) {
        byMonth.set(m.month, (byMonth.get(m.month) ?? 0) + m.netUsd);
      }
      return [...byMonth.entries()]
        .sort(([a], [b]) => a.localeCompare(b))
        .map(([month, value]) => ({label: month, value}));
    }),

    categoryChartData: computed((): DonutSegment[] =>
      (store.data()?.topCategories ?? []).map(c => ({
        label: categoryStore.labelMap()[c.category] ?? MerchantCategoryUtils.format(c.category),
        value: c.totalSpend,
      }))
    ),

    netWorthHistoryData: computed((): ChartPoint[] =>
      store.netWorthHistory().map(s => ({
        label: MONTH_FORMATTER.format(new Date(s.snapshotDate)),
        value: s.totalNetWorth,
      }))
    ),

    isHistoryLoading: computed(() => store.historyLoading()),
    historyErrorMessage: computed(() => store.historyError()),
  };
}
