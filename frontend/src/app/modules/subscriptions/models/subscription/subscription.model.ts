export type SubscriptionStatus = 'active' | 'dismissed' | 'potentially_cancelled';
export type SubscriptionSort = 'date' | 'amount' | 'name';
export type DismissedSubscription = Extract<SubscriptionStatus, 'dismissed'>;

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
