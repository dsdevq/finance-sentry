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

function sortBy(items: Subscription[], sort: SubscriptionSort): Subscription[] {
  return [...items].sort((a, b) => {
    if (sort === 'amount') {
      return b.monthlyEquivalent - a.monthlyEquivalent;
    }
    if (sort === 'name') {
      return a.merchantName.localeCompare(b.merchantName);
    }
    return a.nextExpectedDate.localeCompare(b.nextExpectedDate);
  });
}

const isSubscription = (s: Subscription): boolean => s.kind === 'subscription';
const isInstallment = (s: Subscription): boolean => s.kind === 'installment';

export function subscriptionsComputed(store: StateSignals) {
  return {
    activeSubscriptions: computed(() =>
      store.subscriptions().filter(s => s.status === 'active' && isSubscription(s))
    ),
    dismissedSubscriptions: computed(() =>
      store.subscriptions().filter(s => s.status === 'dismissed' && isSubscription(s))
    ),
    potentiallyCancelledSubscriptions: computed(() =>
      store.subscriptions().filter(s => s.status === 'potentially_cancelled' && isSubscription(s))
    ),
    activeInstallments: computed(() =>
      store.subscriptions().filter(s => s.status === 'active' && isInstallment(s))
    ),
    completedInstallments: computed(() =>
      store.subscriptions().filter(s => s.status === 'completed' && isInstallment(s))
    ),
    dismissTarget: computed(
      () => store.subscriptions().find(s => s.id === store.dismissTargetId()) ?? null
    ),
    sortedActive: computed((): Subscription[] =>
      sortBy(
        store.subscriptions().filter(s => s.status === 'active' && isSubscription(s)),
        store.sort()
      )
    ),
    sortedInstallments: computed((): Subscription[] =>
      sortBy(
        store.subscriptions().filter(s => s.status === 'active' && isInstallment(s)),
        'amount'
      )
    ),
    sortedDismissed: computed((): Subscription[] =>
      sortBy(
        store.subscriptions().filter(s => s.status === 'dismissed' && isSubscription(s)),
        store.sort()
      )
    ),
  };
}
