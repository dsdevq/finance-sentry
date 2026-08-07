import {computed, type Signal} from '@angular/core';
import {type DonutSegment} from '@dsdevq-common/ui';

import {
  type AccountCategory,
  type CategorySummary,
  type NetWorthBreakdownRow,
  type WealthSummaryResponse,
} from '../../../../shared/models/wealth/wealth.model';

const PERCENT = 100;

interface StateSignals {
  summary: Signal<Nullable<WealthSummaryResponse>>;
  status: Signal<'idle' | 'loading' | 'success' | 'error'>;
}

const CATEGORY_LABEL: Record<AccountCategory, string> = {
  banking: 'Banking',
  brokerage: 'Brokerage',
  crypto: 'Crypto',
  other: 'Other',
};

const CATEGORY_COLOR: Record<AccountCategory, string> = {
  banking: '#10b981',
  brokerage: '#6366f1',
  crypto: '#f59e0b',
  other: '#64748b',
};

export function accountsComputed(store: StateSignals) {
  return {
    isEmpty: computed(
      () => store.status() === 'success' && (store.summary()?.categories ?? []).length === 0
    ),
    totalNetWorth: computed(() => store.summary()?.totalNetWorth ?? 0),
    baseCurrency: computed(() => store.summary()?.baseCurrency ?? 'USD'),
    bankingCategory: computed<Nullable<CategorySummary>>(
      () => store.summary()?.categories.find(c => c.category === 'banking') ?? null
    ),
    cryptoCategory: computed<Nullable<CategorySummary>>(
      () => store.summary()?.categories.find(c => c.category === 'crypto') ?? null
    ),
    brokerageCategory: computed<Nullable<CategorySummary>>(
      () => store.summary()?.categories.find(c => c.category === 'brokerage') ?? null
    ),
    totalConnections: computed(
      () => store.summary()?.categories.reduce((sum, cat) => sum + cat.institutionCount, 0) ?? 0
    ),
    netWorthSegments: computed((): DonutSegment[] =>
      (store.summary()?.categories ?? [])
        .filter(cat => cat.totalInBaseCurrency > 0)
        .map(cat => ({
          label: CATEGORY_LABEL[cat.category],
          value: cat.totalInBaseCurrency,
          color: CATEGORY_COLOR[cat.category],
        }))
    ),
    netWorthBreakdown: computed((): NetWorthBreakdownRow[] => {
      const positive = (store.summary()?.categories ?? []).filter(
        cat => cat.totalInBaseCurrency > 0
      );
      const total = positive.reduce((sum, cat) => sum + cat.totalInBaseCurrency, 0);
      return positive
        .map(cat => ({
          label: CATEGORY_LABEL[cat.category],
          color: CATEGORY_COLOR[cat.category],
          value: cat.totalInBaseCurrency,
          institutionCount: cat.institutionCount,
          percent: total > 0 ? (cat.totalInBaseCurrency / total) * PERCENT : 0,
        }))
        .sort((a, b) => b.value - a.value);
    }),
  };
}
