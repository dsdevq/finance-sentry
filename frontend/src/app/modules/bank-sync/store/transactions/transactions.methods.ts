import {type Signal} from '@angular/core';
import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type TransactionListResponse} from '../../models/transaction/transaction.model';
import {
  type TransactionDateRangeFilter,
  type TransactionProviderFilter,
} from './transactions.constants';
import {type TransactionsState} from './transactions.state';

const BANKING_PROVIDERS = new Set<TransactionProviderFilter>(['monobank', 'truelayer']);

interface TransactionsMethodsStore extends WritableStateSource<TransactionsState> {
  selectedProviders: Signal<TransactionProviderFilter[]>;
  selectedCategories: Signal<string[]>;
}

function isBankingTransaction(
  provider: Nullable<string> | undefined,
  accountType: Nullable<string> | undefined
): boolean {
  const normalizedAccountType = accountType?.trim().toLowerCase();
  if (normalizedAccountType) {
    return normalizedAccountType === 'banking';
  }

  const normalizedProvider = provider?.trim().toLowerCase();
  if (normalizedProvider) {
    return BANKING_PROVIDERS.has(normalizedProvider as TransactionProviderFilter);
  }

  return true;
}

export function transactionsMethods(store: TransactionsMethodsStore) {
  return {
    setAccountId(accountId: string): void {
      patchState(store, {accountId});
    },
    setResponse(res: TransactionListResponse): void {
      patchState(store, {
        transactions: res.items.filter(tx => isBankingTransaction(tx.provider, tx.accountType)),
        bankName: res.bankName,
        currency: res.currency,
      });
    },
    toggleProvider(provider: TransactionProviderFilter): void {
      const selected = store.selectedProviders();
      patchState(store, {
        selectedProviders: selected.includes(provider)
          ? selected.filter(current => current !== provider)
          : [...selected, provider],
      });
    },
    toggleCategory(category: string): void {
      const selected = store.selectedCategories();
      patchState(store, {
        selectedCategories: selected.includes(category)
          ? selected.filter(current => current !== category)
          : [...selected, category],
      });
    },
    setDateRange(range: TransactionDateRangeFilter): void {
      patchState(store, {selectedDateRange: range});
    },
  };
}
