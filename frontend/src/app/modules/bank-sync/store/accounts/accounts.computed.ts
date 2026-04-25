import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/ui';

import {
  type CategorySummary,
  type WealthSummaryResponse,
} from '../../../../shared/models/wealth/wealth.model';

interface StateSignals {
  summary: Signal<Nullable<WealthSummaryResponse>>;
  status: Signal<AsyncStatus>;
  errorCode: Signal<Nullable<string>>;
}

const DEFAULT_ERROR_MESSAGE = 'Failed to load accounts. Please try again.';

export function accountsComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isLoading: computed(() => store.status() === 'loading'),
    isEmpty: computed(
      () => store.status() === 'idle' && (store.summary()?.categories ?? []).length === 0
    ),
    errorMessage: computed(() => {
      if (store.status() !== 'error') {
        return '';
      }
      return errorMessages.resolve(store.errorCode()) ?? DEFAULT_ERROR_MESSAGE;
    }),
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
