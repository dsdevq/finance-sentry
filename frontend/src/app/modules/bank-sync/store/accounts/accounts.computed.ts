import {computed, type Signal} from '@angular/core';

import {
  type CategorySummary,
  type WealthSummaryResponse,
} from '../../../../shared/models/wealth/wealth.model';

interface StateSignals {
  summary: Signal<Nullable<WealthSummaryResponse>>;
  status: Signal<'idle' | 'loading' | 'success' | 'error'>;
}

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
      () => store.summary()?.categories.reduce((sum, cat) => sum + cat.accounts.length, 0) ?? 0
    ),
  };
}
