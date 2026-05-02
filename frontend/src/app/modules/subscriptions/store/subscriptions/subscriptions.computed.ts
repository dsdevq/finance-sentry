import {computed, type Signal} from '@angular/core';

import {
  type Subscription,
  type SubscriptionSort,
} from '../../models/subscription/subscription.model';

const MONTHS_PER_YEAR = 12;

interface StateSignals {
  subscriptions: Signal<Subscription[]>;
  sort: Signal<SubscriptionSort>;
  cancelTargetId: Signal<Nullable<string>>;
}

export function subscriptionsComputed(store: StateSignals) {
  return {
    activeSubscriptions: computed(() => store.subscriptions().filter(s => s.status === 'active')),
    pausedSubscriptions: computed(() => store.subscriptions().filter(s => s.status === 'paused')),
    monthlyTotal: computed(() =>
      store
        .subscriptions()
        .filter(s => s.status === 'active')
        .reduce((sum, s) => sum + s.amount, 0)
    ),
    yearlyTotal: computed(
      () =>
        store
          .subscriptions()
          .filter(s => s.status === 'active')
          .reduce((sum, s) => sum + s.amount, 0) * MONTHS_PER_YEAR
    ),
    cancelTarget: computed(
      () => store.subscriptions().find(s => s.id === store.cancelTargetId()) ?? null
    ),
    sortedActive: computed((): Subscription[] => {
      const active = store.subscriptions().filter(s => s.status === 'active');
      const sort = store.sort();
      return [...active].sort((a, b) => {
        if (sort === 'amount') {
          return b.amount - a.amount;
        }
        if (sort === 'name') {
          return a.name.localeCompare(b.name);
        }
        return a.nextDate.localeCompare(b.nextDate);
      });
    }),
    sortedPaused: computed((): Subscription[] => {
      const paused = store.subscriptions().filter(s => s.status === 'paused');
      const sort = store.sort();
      return [...paused].sort((a, b) => {
        if (sort === 'amount') {
          return b.amount - a.amount;
        }
        if (sort === 'name') {
          return a.name.localeCompare(b.name);
        }
        return a.nextDate.localeCompare(b.nextDate);
      });
    }),
  };
}
