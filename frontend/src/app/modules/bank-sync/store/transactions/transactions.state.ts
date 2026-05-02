import {type Transaction} from '../../models/transaction/transaction.model';

export interface TransactionsFilter {
  accountId: string;
  startDate: string;
  endDate: string;
  offset: number;
}

export interface TransactionsState {
  accountId: string;
  bankName: string;
  currency: string;
  transactions: Transaction[];
  startDate: string;
  endDate: string;
}

export const PAGE_SIZE = 50;

export const initialTransactionsState: TransactionsState = {
  accountId: '',
  bankName: '',
  currency: '',
  transactions: [],
  startDate: '',
  endDate: '',
};
