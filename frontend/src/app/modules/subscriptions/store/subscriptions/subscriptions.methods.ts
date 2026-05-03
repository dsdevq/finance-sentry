import {patchState, type WritableStateSource} from '@ngrx/signals';

import {
  type Subscription,
  type SubscriptionSort,
  type SubscriptionSummary,
} from '../../models/subscription/subscription.model';
import {type SubscriptionsState} from './subscriptions.state';

export function subscriptionsMethods(store: WritableStateSource<SubscriptionsState>) {
  return {
    setData(subscriptions: Subscription[], hasInsufficientHistory: boolean): void {
      patchState(store, {subscriptions, hasInsufficientHistory, status: 'idle'});
    },
    setSummary(summary: SubscriptionSummary): void {
      patchState(store, {summary});
    },
    setSort(sort: SubscriptionSort): void {
      patchState(store, {sort});
    },
    setDismissTarget(id: Nullable<string>): void {
      patchState(store, {dismissTargetId: id});
    },
    confirmDismiss(): void {
      patchState(store, (s: SubscriptionsState) => ({
        subscriptions: s.subscriptions.map(sub =>
          sub.id === s.dismissTargetId ? {...sub, status: 'dismissed' as const} : sub
        ),
        dismissTargetId: null,
        summary: null,
      }));
    },
    restoreSubscription(id: string): void {
      patchState(store, (s: SubscriptionsState) => ({
        subscriptions: s.subscriptions.map(sub =>
          sub.id === id ? {...sub, status: 'active' as const} : sub
        ),
        summary: null,
      }));
    },
  };
}
