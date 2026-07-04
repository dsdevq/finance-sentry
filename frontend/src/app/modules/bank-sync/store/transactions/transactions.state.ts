import {type Transaction} from '../../models/transaction/transaction.model';
import {
  type TransactionDateRangeFilter,
  type TransactionProviderFilter,
} from './transactions.constants';

export interface TransactionsState {
  accountId: string;
  bankName: string;
  currency: string;
  transactions: Transaction[];
  selectedProviders: TransactionProviderFilter[];
  selectedCategories: string[];
  selectedDateRange: TransactionDateRangeFilter;
}

export const PAGE_SIZE = 50;

export const initialTransactionsState: TransactionsState = {
  accountId: '',
  bankName: '',
  currency: '',
  transactions: [],
  selectedProviders: [],
  selectedCategories: [],
  selectedDateRange: 'all',
};
