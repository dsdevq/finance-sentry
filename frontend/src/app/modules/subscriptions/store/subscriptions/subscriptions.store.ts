import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {subscriptionsComputed} from './subscriptions.computed';
import {subscriptionsEffects, subscriptionsHooks} from './subscriptions.effects';
import {subscriptionsMethods} from './subscriptions.methods';
import {initialSubscriptionsState} from './subscriptions.state';

export const SubscriptionsStore = signalStore(
  withState(initialSubscriptionsState),
  withMethods(subscriptionsMethods),
  withComputed(subscriptionsComputed),
  withMethods(subscriptionsEffects),
  withHooks({onInit: subscriptionsHooks})
);
