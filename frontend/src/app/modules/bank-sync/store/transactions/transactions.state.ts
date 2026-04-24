import {type Transaction} from '../../models/transaction.model';

export type TransactionsStatus = 'idle' | 'loading' | 'error';

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
  totalCount: number;
  offset: number;
  startDate: string;
  endDate: string;
  status: TransactionsStatus;
  errorCode: string | null;
}

export const PAGE_SIZE = 50;

export const initialTransactionsState: TransactionsState = {
  accountId: '',
  bankName: '',
  currency: '',
  transactions: [],
  totalCount: 0,
  offset: 0,
  startDate: '',
  endDate: '',
  status: 'idle',
  errorCode: null,
};
