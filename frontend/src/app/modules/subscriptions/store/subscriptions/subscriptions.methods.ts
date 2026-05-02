import {patchState, type WritableStateSource} from '@ngrx/signals';

import {
  type Subscription,
  type SubscriptionSort,
} from '../../models/subscription/subscription.model';
import {type SubscriptionsState} from './subscriptions.state';

export function subscriptionsMethods(store: WritableStateSource<SubscriptionsState>) {
  return {
    setData(subscriptions: Subscription[]): void {
      patchState(store, {subscriptions, status: 'idle'});
    },
    setSort(sort: SubscriptionSort): void {
      patchState(store, {sort});
    },
    toggleStatus(id: string): void {
      patchState(store, (s: SubscriptionsState) => ({
        subscriptions: s.subscriptions.map(sub =>
          sub.id === id ? {...sub, status: sub.status === 'active' ? 'paused' : 'active'} : sub
        ),
      }));
    },
    setCancelTarget(id: Nullable<string>): void {
      patchState(store, {cancelTargetId: id});
    },
    confirmCancel(): void {
      patchState(store, (s: SubscriptionsState) => ({
        subscriptions: s.subscriptions.filter(sub => sub.id !== s.cancelTargetId),
        cancelTargetId: null,
      }));
    },
  };
}
