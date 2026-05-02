import {computed, inject, type Signal} from '@angular/core';
import {type CmnTablePagination, ErrorMessageService} from '@dsdevq-common/ui';

import {PAGE_SIZE} from './transactions.state';

interface StateSignals {
  status: Signal<AsyncStatus>;
  errorCode: Signal<Nullable<string>>;
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
    pagination: computed<CmnTablePagination>(() => ({
      totalCount: store.totalCount(),
      offset: store.offset(),
      limit: PAGE_SIZE,
      hasMore: store.offset() + PAGE_SIZE < store.totalCount(),
    })),
  };
}
