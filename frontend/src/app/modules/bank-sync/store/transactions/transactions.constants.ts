export type TransactionProviderFilter = 'monobank' | 'truelayer';

export type TransactionDateRangeFilter = 'all' | 'last7' | 'last30' | 'thisMonth';

export interface TransactionFilterOption<T> {
  readonly value: T;
  readonly label: string;
}

export const TRANSACTION_PROVIDER_FILTER_OPTIONS: readonly TransactionFilterOption<TransactionProviderFilter>[] =
  [
    {value: 'monobank', label: 'Monobank'},
    {value: 'truelayer', label: 'TrueLayer'},
  ];

export const TRANSACTION_DATE_RANGE_FILTER_OPTIONS: readonly TransactionFilterOption<TransactionDateRangeFilter>[] =
  [
    {value: 'all', label: 'All'},
    {value: 'last7', label: 'Last 7 days'},
    {value: 'last30', label: 'Last 30 days'},
    {value: 'thisMonth', label: 'This month'},
  ];
