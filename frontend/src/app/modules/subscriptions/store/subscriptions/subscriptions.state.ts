import {
  type Subscription,
  type SubscriptionSort,
  type SubscriptionSummary,
} from '../../models/subscription/subscription.model';

export interface SubscriptionsState {
  subscriptions: Subscription[];
  sort: SubscriptionSort;
  dismissTargetId: Nullable<string>;
  summary: Nullable<SubscriptionSummary>;
  hasInsufficientHistory: boolean;
  status: AsyncStatus;
}

export const initialSubscriptionsState: SubscriptionsState = {
  subscriptions: [],
  sort: 'date',
  dismissTargetId: null,
  summary: null,
  hasInsufficientHistory: false,
  status: 'idle',
};
