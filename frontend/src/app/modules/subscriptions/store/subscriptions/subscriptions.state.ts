import {
  type InstallmentFxImpactResponse,
  type Subscription,
  type SubscriptionSort,
  type SubscriptionSummary,
} from '../../models/subscription/subscription.model';

export interface SubscriptionsState {
  subscriptions: Subscription[];
  sort: SubscriptionSort;
  summary: Nullable<SubscriptionSummary>;
  fxImpact: Nullable<InstallmentFxImpactResponse>;
  hasInsufficientHistory: boolean;
  status: AsyncStatus;
}

export const initialSubscriptionsState: SubscriptionsState = {
  subscriptions: [],
  sort: 'date',
  summary: null,
  fxImpact: null,
  hasInsufficientHistory: false,
  status: 'idle',
};
