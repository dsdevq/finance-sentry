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

export interface SubscriptionSummary {
  totalMonthlyEstimate: number;
  totalAnnualEstimate: number;
  activeCount: number;
  potentiallyCancelledCount: number;
  currency: string;
}

export interface SubscriptionsListResponse {
  items: Subscription[];
  totalCount: number;
  hasInsufficientHistory: boolean;
}
