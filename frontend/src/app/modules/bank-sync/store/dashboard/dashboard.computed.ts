import {CurrencyPipe} from '@angular/common';
import {computed, inject, type Signal} from '@angular/core';
import {type ChartPoint, type ChartSeries, type DonutSegment} from '@dsdevq-common/ui';

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

export function dashboardComputed(store: StateSignals) {
  const currency = inject(CurrencyPipe);

  return {
    currencyEntries: computed(() => Object.entries(store.data()?.aggregatedBalance ?? {})),
    accountTypeEntries: computed(() => Object.entries(store.data()?.accountsByType ?? {})),

    totalBalanceFormatted: computed(
      () => currency.transform(store.data()?.totalNetWorthUsd ?? 0) ?? ''
    ),

    latestInflowFormatted: computed(() => {
      const flows = store.data()?.monthlyFlow ?? [];
      const latest = flows[flows.length - 1];
      return latest ? COMPACT_FORMATTER.format(latest.inflow) : '—';
    }),

    latestOutflowFormatted: computed(() => {
      const flows = store.data()?.monthlyFlow ?? [];
      const latest = flows[flows.length - 1];
      return latest ? COMPACT_FORMATTER.format(latest.outflow) : '—';
    }),

    netFlowChartData: computed((): ChartPoint[] =>
      (store.data()?.monthlyFlow ?? []).map(m => ({
        label: m.month,
        value: m.net,
      }))
    ),

    categoryChartData: computed((): DonutSegment[] =>
      (store.data()?.topCategories ?? []).map(c => ({
        label: MerchantCategoryUtils.format(c.category),
        value: c.totalSpend,
      }))
    ),

    netWorthHistoryData: computed((): ChartPoint[] =>
      store.netWorthHistory().map(s => ({
        label: MONTH_FORMATTER.format(new Date(s.snapshotDate)),
        value: s.totalNetWorth,
      }))
    ),

    netWorthSeriesData: computed((): ChartSeries[] => {
      const snapshots = store.netWorthHistory();
      if (snapshots.length === 0) {
        return [];
      }
      const labels = snapshots.map(s => MONTH_FORMATTER.format(new Date(s.snapshotDate)));
      return [
        {
          label: 'Banking',
          color: '#4f46e5',
          data: snapshots.map((s, i) => ({label: labels[i], value: s.bankingTotal})),
        },
        {
          label: 'Brokerage',
          color: '#10b981',
          data: snapshots.map((s, i) => ({label: labels[i], value: s.brokerageTotal})),
        },
        {
          label: 'Crypto',
          color: '#f59e0b',
          data: snapshots.map((s, i) => ({label: labels[i], value: s.cryptoTotal})),
        },
      ];
    }),

    isHistoryLoading: computed(() => store.historyLoading()),
    historyErrorMessage: computed(() => store.historyError()),
  };
}
