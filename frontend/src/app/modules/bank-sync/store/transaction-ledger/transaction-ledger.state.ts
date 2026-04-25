import {type GlobalTransactionDto} from '../../models/transaction.model';

export type LedgerStatus = 'idle' | 'loading' | 'error';

export interface TransactionLedgerState {
  transactions: GlobalTransactionDto[];
  totalCount: number;
  hasMore: boolean;
  offset: number;
  status: LedgerStatus;
  errorCode: string | null;
}

export const PAGE_SIZE = 50;

export const initialTransactionLedgerState: TransactionLedgerState = {
  transactions: [],
  totalCount: 0,
  hasMore: false,
  offset: 0,
  status: 'idle',
  errorCode: null,
};
