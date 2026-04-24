import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/ui';

import {PAGE_SIZE, type TransactionsStatus} from './transactions.state';

interface StateSignals {
  status: Signal<TransactionsStatus>;
  errorCode: Signal<string | null>;
  offset: Signal<number>;
  totalCount: Signal<number>;
}

const DEFAULT_ERROR = 'Failed to load transactions. Please try again.';

export function transactionsComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isLoading: computed(() => store.status() === 'loading'),
    errorMessage: computed(() => {
      if (store.status() !== 'error') {
        return '';
      }
      return errorMessages.resolve(store.errorCode()) ?? DEFAULT_ERROR;
    }),
    currentPage: computed(() => Math.floor(store.offset() / PAGE_SIZE) + 1),
    totalPages: computed(() => Math.max(1, Math.ceil(store.totalCount() / PAGE_SIZE))),
    hasPrevious: computed(() => store.offset() > 0),
    hasNext: computed(() => store.offset() + PAGE_SIZE < store.totalCount()),
  };
}
