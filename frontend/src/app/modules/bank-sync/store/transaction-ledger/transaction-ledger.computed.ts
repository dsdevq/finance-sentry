import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/ui';

import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';

interface StateSignals {
  transactions: Signal<GlobalTransactionDto[]>;
  totalCount: Signal<number>;
  hasMore: Signal<boolean>;
  status: Signal<AsyncStatus>;
  errorCode: Signal<Nullable<string>>;
}

const DEFAULT_ERROR = 'Failed to load transactions. Please try again.';

export function transactionLedgerComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isLoading: computed(() => store.status() === 'loading'),
    isEmpty: computed(() => store.status() === 'idle' && store.transactions().length === 0),
    errorMessage: computed(() => {
      if (store.status() !== 'error') {
        return '';
      }
      return errorMessages.resolve(store.errorCode()) ?? DEFAULT_ERROR;
    }),
    monthlyOutflow: computed(() => {
      const now = new Date();
      const monthStart = new Date(now.getFullYear(), now.getMonth(), 1);
      return store
        .transactions()
        .filter(t => {
          const date = new Date(t.postedDate ?? t.date);
          return t.transactionType === 'debit' && date >= monthStart;
        })
        .reduce((sum, t) => sum + Math.abs(t.amount), 0);
    }),
    topCategory: computed(() => {
      const counts: Record<string, number> = {};
      for (const t of store.transactions()) {
        const cat = t.merchantCategory ?? 'Uncategorized';
        counts[cat] = (counts[cat] ?? 0) + 1;
      }
      const entries = Object.entries(counts);
      if (entries.length === 0) {
        return '—';
      }
      return entries.reduce((a, b) => (a[1] >= b[1] ? a : b))[0];
    }),
  };
}
