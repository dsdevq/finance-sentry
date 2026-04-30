import {type PagedRequest, type PagedResponse} from '../../../../shared/models/api/api.model';
import {type Timestamped} from '../../../../shared/models/timestamped/timestamped.model';

export type TransactionType = 'debit' | 'credit';

export interface GlobalTransactionDto extends Timestamped {
  transactionId: string;
  accountId: string;
  bankName: string;
  amount: number;
  date: string;
  postedDate: Nullable<string>;
  description: string;
  transactionType: Nullable<TransactionType>;
  merchantCategory: Nullable<string>;
  isPending: boolean;
}

export type GlobalTransactionsResponse = PagedResponse<GlobalTransactionDto>;

export interface Transaction extends Timestamped {
  transactionId: string;
  accountId: string;
  amount: number;
  transactionType: TransactionType;
  postedDate: Nullable<string>;
  pendingDate: Nullable<string>;
  isPending: boolean;
  description: string;
  merchantCategory: Nullable<string>;
  syncedAt: string;
}

export interface TransactionListResponse extends PagedResponse<Transaction> {
  accountId: string;
  bankName: string;
  currency: string;
}

export interface TransactionQueryParams extends PagedRequest {
  startDate?: string;
  endDate?: string;
  status?: 'posted' | 'pending' | 'all';
}

export interface GetAllTransactionsParams extends PagedRequest {
  from?: string;
  to?: string;
}
