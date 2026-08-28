export type SubscriptionStatus = 'active' | 'dismissed' | 'potentially_cancelled' | 'completed';
export type SubscriptionSort = 'date' | 'amount' | 'name';
export type DismissedSubscription = Extract<SubscriptionStatus, 'dismissed'>;
export type SubscriptionKind = 'subscription' | 'installment';

export interface Subscription {
  id: string;
  merchantName: string;
  cadence: 'monthly' | 'annual';
  averageAmount: number;
  lastKnownAmount: number;
  monthlyEquivalent: number;
  currency: string;
  lastChargeDate: string;
  nextExpectedDate: string;
  status: SubscriptionStatus;
  occurrenceCount: number;
  kind: SubscriptionKind;
  termCount: Nullable<number>;
  endDate: Nullable<string>;
  /** User-set plan start; detection only sees ~13 months, so an older plan needs this. */
  startDate: Nullable<string>;
  remainingPayments: Nullable<number>;
  isManual: boolean;
}

export interface AddInstallmentRequest {
  merchant: string;
  monthlyAmount: number;
  currency: string;
  startDate: string;
  termCount: Nullable<number>;
}

export interface AddSubscriptionRequest {
  merchant: string;
  monthlyAmount: number;
  currency: string;
  startDate: string;
}

export interface SpendBucket {
  /** Current monthly run-rate. */
  monthly: number;
  /**
   * What actually leaves the account over the next 12 months. Subscriptions are open-ended
   * (`monthly × 12`); an installment contributes only its remaining payments, capped at 12.
   */
  next12Months: number;
  /** Total still owed until every plan ends — null for open-ended buckets. */
  remainingCommitment: Nullable<number>;
  activeCount: number;
  /** A plan in this bucket has no term or end date, so the figures assume it continues. */
  hasUnknownSchedule: boolean;
}

export interface SubscriptionSummary {
  subscriptions: SpendBucket;
  installments: SpendBucket;
  combined: SpendBucket;
  potentiallyCancelledCount: number;
  currency: string;
}

/** What exchange-rate movement has done to one foreign-currency plan. */
export interface InstallmentFxImpact {
  id: string;
  merchant: string;
  currency: string;
  /** Contractually fixed — only its cost in the base currency moves. */
  monthlyNative: number;
  baselineDate: string;
  baselineUnitsPerBase: number;
  baselineCost: number;
  currentDate: string;
  currentUnitsPerBase: number;
  currentCost: number;
  changeAmount: number;
  changePercent: number;
  /** True when the baseline is only the first observed charge, not the plan's real start. */
  baselineIsObserved: boolean;
}

export interface FxCostPoint {
  date: string;
  unitsPerBase: number;
  monthlyCost: number;
}

export interface InstallmentFxImpactResponse {
  baseCurrency: string;
  plans: InstallmentFxImpact[];
  baselineCostTotal: number;
  currentCostTotal: number;
  changeAmountTotal: number;
  changePercentTotal: number;
  points: FxCostPoint[];
}

export interface SubscriptionsListResponse {
  items: Subscription[];
  totalCount: number;
  hasInsufficientHistory: boolean;
}
