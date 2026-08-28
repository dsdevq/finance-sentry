import {computed, type Signal} from '@angular/core';
import {type ChartPoint} from '@lifekit-hq/ui';

import {
  type InstallmentFxImpactResponse,
  type Subscription,
  type SubscriptionSort,
  type SubscriptionSummary,
} from '../../models/subscription/subscription.model';

interface StateSignals {
  subscriptions: Signal<Subscription[]>;
  sort: Signal<SubscriptionSort>;
  summary: Signal<Nullable<SubscriptionSummary>>;
  fxImpact: Signal<Nullable<InstallmentFxImpactResponse>>;
}

const MONTH_LABEL_OPTIONS: Intl.DateTimeFormatOptions = {month: 'short', year: '2-digit'};

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
    // Monthly cost of the foreign-currency plans in the base currency: the payments are
    // fixed, so every move in this line is the exchange rate, not a change in what's owed.
    fxCostPoints: computed((): ChartPoint[] =>
      (store.fxImpact()?.points ?? []).map(p => ({
        label: new Date(p.date).toLocaleDateString(undefined, MONTH_LABEL_OPTIONS),
        value: p.monthlyCost,
      }))
    ),
    fxRatePoints: computed((): ChartPoint[] =>
      (store.fxImpact()?.points ?? []).map(p => ({
        label: new Date(p.date).toLocaleDateString(undefined, MONTH_LABEL_OPTIONS),
        value: p.unitsPerBase,
      }))
    ),
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
