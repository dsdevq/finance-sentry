import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/ui';

import {
  type AccountBalanceItem,
  type CategorySummary,
} from '../../../shared/models/wealth/wealth.model';
import {type HoldingsState} from './holdings.state';

interface StateSignals {
  summary: Signal<HoldingsState['summary']>;
  status: Signal<HoldingsState['status']>;
  errorCode: Signal<Nullable<string>>;
}

const DEFAULT_ERROR = 'Failed to load holdings. Please try again.';

export function holdingsComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isLoading: computed(() => store.status() === 'loading'),
    errorMessage: computed(() => {
      if (store.status() !== 'error') {
        return '';
      }
      return errorMessages.resolve(store.errorCode()) ?? DEFAULT_ERROR;
    }),
    totalNetWorth: computed(() => store.summary()?.totalNetWorth ?? 0),
    baseCurrency: computed(() => store.summary()?.baseCurrency ?? 'USD'),
    bankingCategory: computed(
      (): Nullable<CategorySummary> =>
        store.summary()?.categories.find(c => c.category === 'banking') ?? null
    ),
    brokerageCategory: computed(
      (): Nullable<CategorySummary> =>
        store.summary()?.categories.find(c => c.category === 'brokerage') ?? null
    ),
    cryptoCategory: computed(
      (): Nullable<CategorySummary> =>
        store.summary()?.categories.find(c => c.category === 'crypto') ?? null
    ),
    allAccounts: computed(
      (): AccountBalanceItem[] => store.summary()?.categories.flatMap(c => c.accounts) ?? []
    ),
  };
}
