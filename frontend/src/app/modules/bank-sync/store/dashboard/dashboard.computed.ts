import {computed, inject, type Signal} from '@angular/core';
import {type ChartPoint, type DonutSegment, ErrorMessageService} from '@dsdevq-common/ui';

import {type DashboardData} from '../../models/dashboard.model';
import {type DashboardStatus} from './dashboard.state';

interface StateSignals {
  data: Signal<DashboardData | null>;
  status: Signal<DashboardStatus>;
  errorCode: Signal<string | null>;
}

const DEFAULT_ERROR_MESSAGE = 'Failed to load dashboard data. Please try again.';

const USD_FORMATTER = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const COMPACT_FORMATTER = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  notation: 'compact',
  minimumFractionDigits: 0,
  maximumFractionDigits: 1,
});

export function dashboardComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isLoading: computed(() => store.status() === 'loading'),
    errorMessage: computed(() => {
      if (store.status() !== 'error') {
        return '';
      }
      return errorMessages.resolve(store.errorCode()) ?? DEFAULT_ERROR_MESSAGE;
    }),
    currencyEntries: computed(() => Object.entries(store.data()?.aggregatedBalance ?? {})),
    accountTypeEntries: computed(() => Object.entries(store.data()?.accountsByType ?? {})),

    totalBalanceFormatted: computed(() => {
      const bal = store.data()?.aggregatedBalance ?? {};
      const usd = bal['USD'] ?? Object.values(bal)[0] ?? 0;
      return USD_FORMATTER.format(usd);
    }),

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

    netFlowChartData: computed((): ChartPoint[] => {
      return (store.data()?.monthlyFlow ?? []).map(m => ({
        label: m.month,
        value: m.net,
      }));
    }),

    categoryChartData: computed((): DonutSegment[] => {
      return (store.data()?.topCategories ?? []).map(c => ({
        label: c.category,
        value: c.totalSpend,
      }));
    }),
  };
}
