export type TransactionType = 'debit' | 'credit';

export interface GlobalTransactionDto {
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
  createdAt: string;
}

export interface GlobalTransactionsResponse {
  transactions: GlobalTransactionDto[];
  totalCount: number;
  hasMore: boolean;
}

export interface Transaction {
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
  createdAt: string;
}

export interface TransactionListResponse {
  accountId: string;
  bankName: string;
  currency: string;
  transactions: Transaction[];
  pagination: OffsetPagination;
}

export interface TransactionQueryParams extends OffsetPaginationParams {
  startDate?: string;
  endDate?: string;
  status?: 'posted' | 'pending' | 'all';
  sort?: string;
}
