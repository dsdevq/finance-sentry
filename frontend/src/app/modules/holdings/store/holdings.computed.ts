import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/ui';

import {type AccountBalanceItem, type CategorySummary} from '../../bank-sync/models/wealth.model';
import {type HoldingsState} from './holdings.state';

interface StateSignals {
  summary: Signal<HoldingsState['summary']>;
  status: Signal<HoldingsState['status']>;
  errorCode: Signal<string | null>;
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
      (): CategorySummary | null =>
        store.summary()?.categories.find(c => c.category === 'Banking') ?? null
    ),
    brokerageCategory: computed(
      (): CategorySummary | null =>
        store.summary()?.categories.find(c => c.category === 'Brokerage') ?? null
    ),
    cryptoCategory: computed(
      (): CategorySummary | null =>
        store.summary()?.categories.find(c => c.category === 'Crypto') ?? null
    ),
    allAccounts: computed(
      (): AccountBalanceItem[] => store.summary()?.categories.flatMap(c => c.accounts) ?? []
    ),
  };
}
