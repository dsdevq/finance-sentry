import {computed, type Signal} from '@angular/core';

import {type Transaction} from '../../models/transaction/transaction.model';
import {
  TRANSACTION_CATEGORY_FILTER_OPTIONS,
  TRANSACTION_DATE_RANGE_FILTER_OPTIONS,
  TRANSACTION_PROVIDER_FILTER_OPTIONS,
  type TransactionDateRangeFilter,
  type TransactionProviderFilter,
} from './transactions.constants';

const HOURS_PER_DAY = 24;
const MINUTES_PER_HOUR = 60;
const SECONDS_PER_MINUTE = 60;
const MS_PER_SECOND = 1000;
const MS_PER_DAY = HOURS_PER_DAY * MINUTES_PER_HOUR * SECONDS_PER_MINUTE * MS_PER_SECOND;
const LAST_7_DAYS = 7;
const LAST_30_DAYS = 30;

interface ComputedStore {
  transactions: Signal<Transaction[]>;
  selectedProviders: Signal<TransactionProviderFilter[]>;
  selectedCategories: Signal<string[]>;
  selectedDateRange: Signal<TransactionDateRangeFilter>;
}

function dateRangeCutoff(range: TransactionDateRangeFilter, now: Date): Nullable<Date> {
  switch (range) {
    case 'last7':
      return new Date(now.getTime() - LAST_7_DAYS * MS_PER_DAY);
    case 'last30':
      return new Date(now.getTime() - LAST_30_DAYS * MS_PER_DAY);
    case 'thisMonth':
      return new Date(now.getFullYear(), now.getMonth(), 1);
    default:
      return null;
  }
}

export function transactionsComputed(store: ComputedStore) {
  return {
    providerFilters: computed(() => TRANSACTION_PROVIDER_FILTER_OPTIONS),
    categoryFilters: computed(() => TRANSACTION_CATEGORY_FILTER_OPTIONS),
    dateRangeFilters: computed(() => TRANSACTION_DATE_RANGE_FILTER_OPTIONS),
    filteredTransactions: computed(() => {
      const providers = store.selectedProviders();
      const categories = store.selectedCategories();
      const cutoff = dateRangeCutoff(store.selectedDateRange(), new Date());

      return store.transactions().filter(tx => {
        if (providers.length > 0) {
          const provider = tx.provider?.toLowerCase();
          if (!provider || !providers.includes(provider as TransactionProviderFilter)) {
            return false;
          }
        }
        if (categories.length > 0) {
          const category = tx.merchantCategory?.toLowerCase();
          if (!category || !categories.includes(category)) {
            return false;
          }
        }
        if (cutoff) {
          const dateStr = tx.postedDate ?? tx.pendingDate ?? tx.syncedAt;
          if (new Date(dateStr) < cutoff) {
            return false;
          }
        }
        return true;
      });
    }),
  };
}
