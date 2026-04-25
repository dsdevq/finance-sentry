export type TransactionType = 'debit' | 'credit';

export interface GlobalTransactionDto {
  transactionId: string;
  accountId: string;
  bankName: string;
  amount: number;
  date: string;
  postedDate: string | null;
  description: string;
  transactionType: TransactionType | null;
  merchantCategory: string | null;
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
  postedDate: string | null;
  pendingDate: string | null;
  isPending: boolean;
  description: string;
  merchantCategory: string | null;
  syncedAt: string;
  createdAt: string;
}

export interface TransactionListResponse {
  accountId: string;
  bankName: string;
  currency: string;
  transactions: Transaction[];
  pagination: {
    offset: number;
    limit: number;
    totalCount: number;
    hasMore: boolean;
  };
}

export interface TransactionQueryParams {
  startDate?: string;
  endDate?: string;
  offset?: number;
  limit?: number;
  status?: 'posted' | 'pending' | 'all';
  sort?: string;
}
