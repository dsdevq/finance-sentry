export interface AccountBalanceItem {
  accountId: string;
  bankName: string;
  accountType: string;
  accountNumberLast4: string;
  provider: string;
  category: string;
  currency: string;
  currentBalance: number;
  balanceInBaseCurrency: number | null;
  syncStatus: string;
}

export interface CategorySummary {
  category: string;
  totalInBaseCurrency: number;
  accounts: AccountBalanceItem[];
}

export interface AppliedFilters {
  category: string | null;
  provider: string | null;
}

export interface WealthSummaryResponse {
  totalNetWorth: number;
  baseCurrency: string;
  categories: CategorySummary[];
  appliedFilters: AppliedFilters;
}
