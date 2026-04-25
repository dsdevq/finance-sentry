import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';

export interface TransactionLedgerState {
  transactions: GlobalTransactionDto[];
  totalCount: number;
  hasMore: boolean;
  offset: number;
  status: AsyncStatus;
  errorCode: Nullable<string>;
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
