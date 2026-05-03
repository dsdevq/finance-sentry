import {computed, type Signal} from '@angular/core';

import {
  type Subscription,
  type SubscriptionSort,
  type SubscriptionSummary,
} from '../../models/subscription/subscription.model';

interface StateSignals {
  subscriptions: Signal<Subscription[]>;
  sort: Signal<SubscriptionSort>;
  dismissTargetId: Signal<Nullable<string>>;
  summary: Signal<Nullable<SubscriptionSummary>>;
}

export function subscriptionsComputed(store: StateSignals) {
  return {
    activeSubscriptions: computed(() => store.subscriptions().filter(s => s.status === 'active')),
    dismissedSubscriptions: computed(() =>
      store.subscriptions().filter(s => s.status === 'dismissed')
    ),
    potentiallyCancelledSubscriptions: computed(() =>
      store.subscriptions().filter(s => s.status === 'potentially_cancelled')
    ),
    monthlyTotal: computed(() =>
      store
        .subscriptions()
        .filter(s => s.status === 'active')
        .reduce((sum, s) => sum + s.monthlyEquivalent, 0)
    ),
    dismissTarget: computed(
      () => store.subscriptions().find(s => s.id === store.dismissTargetId()) ?? null
    ),
    sortedActive: computed((): Subscription[] => {
      const active = store.subscriptions().filter(s => s.status === 'active');
      const sort = store.sort();
      return [...active].sort((a, b) => {
        if (sort === 'amount') {
          return b.monthlyEquivalent - a.monthlyEquivalent;
        }
        if (sort === 'name') {
          return a.merchantName.localeCompare(b.merchantName);
        }
        return a.nextExpectedDate.localeCompare(b.nextExpectedDate);
      });
    }),
    sortedDismissed: computed((): Subscription[] => {
      const dismissed = store.subscriptions().filter(s => s.status === 'dismissed');
      const sort = store.sort();
      return [...dismissed].sort((a, b) => {
        if (sort === 'amount') {
          return b.monthlyEquivalent - a.monthlyEquivalent;
        }
        if (sort === 'name') {
          return a.merchantName.localeCompare(b.merchantName);
        }
        return a.nextExpectedDate.localeCompare(b.nextExpectedDate);
      });
    }),
  };
}
