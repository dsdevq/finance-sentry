import {
  type Subscription,
  type SubscriptionSort,
} from '../../models/subscription/subscription.model';

export interface SubscriptionsState {
  subscriptions: Subscription[];
  sort: SubscriptionSort;
  cancelTargetId: Nullable<string>;
  status: AsyncStatus;
}

export const initialSubscriptionsState: SubscriptionsState = {
  subscriptions: [],
  sort: 'date',
  cancelTargetId: null,
  status: 'idle',
};
