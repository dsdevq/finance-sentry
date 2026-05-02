import {CurrencyPipe} from '@angular/common';
import {computed, inject, type Signal} from '@angular/core';
import {type ChartPoint, type DonutSegment} from '@dsdevq-common/ui';

import {MerchantCategoryUtils} from '../../../../shared/utils/merchant-category.utils';
import {type DashboardData, NET_WORTH_HISTORY_MOCK} from '../../models/dashboard/dashboard.model';

interface StateSignals {
  data: Signal<Nullable<DashboardData>>;
}

const COMPACT_FORMATTER = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  notation: 'compact',
  minimumFractionDigits: 0,
  maximumFractionDigits: 1,
});

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
      NET_WORTH_HISTORY_MOCK.map(p => ({label: p.month, value: p.total}))
    ),
  };
}
