import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {forkJoin, pipe, switchMap, tap} from 'rxjs';

import {
  type Subscription,
  type SubscriptionSummary,
} from '../../models/subscription/subscription.model';
import {SubscriptionsService} from '../../services/subscriptions.service';

interface EffectsStore {
  setData: (subscriptions: Subscription[], hasInsufficientHistory: boolean) => void;
  setSummary: (summary: SubscriptionSummary) => void;
  confirmDismiss: () => void;
  restoreSubscription: (id: string) => void;
}

export function subscriptionsEffects(store: EffectsStore) {
  const service = inject(SubscriptionsService);

  return {
    load: rxMethod<void>(
      pipe(
        switchMap(() =>
          forkJoin({
            list$: service.getSubscriptions(true),
            summary$: service.getSummary(),
          })
        ),
        tap(({list$, summary$}) => {
          store.setData(list$.items, list$.hasInsufficientHistory);
          store.setSummary(summary$);
        })
      )
    ),
    dismiss: rxMethod<string>(
      pipe(switchMap(id => service.dismiss(id).pipe(tap(() => store.confirmDismiss()))))
    ),
    restore: rxMethod<string>(
      pipe(switchMap(id => service.restore(id).pipe(tap(() => store.restoreSubscription(id)))))
    ),
  };
}

interface HookStore extends EffectsStore {
  load: () => void;
}

export function subscriptionsHooks(store: HookStore): void {
  store.load();
}
