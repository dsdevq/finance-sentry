/**
 * Classification bucket a transaction landed in for the month's flow figures — the audit
 * labels behind the dashboard tiles. Mirrors `FlowBuckets` on the backend.
 */
export type FlowBucket =
  | 'income'
  | 'spending'
  | 'invested'
  | 'investment-return'
  | 'excluded-pair'
  | 'excluded-routing'
  | 'excluded-transfer';

export interface FlowBreakdownItem {
  transactionId: string;
  accountId: string;
  bankName: string;
  accountLast4: string;
  currency: string;
  amount: number;
  amountUsd: number;
  date: string;
  description: string;
  merchantName: Nullable<string>;
  category: Nullable<string>;
  direction: 'in' | 'out';
  bucket: FlowBucket;
  counterpartyName: Nullable<string>;
  flowRole: Nullable<string>;
}

export interface FlowBreakdown {
  month: string;
  items: FlowBreakdownItem[];
}
