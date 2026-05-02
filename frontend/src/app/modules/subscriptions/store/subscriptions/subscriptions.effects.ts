import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {of, pipe, switchMap, tap} from 'rxjs';

import {SUBSCRIPTION_MOCK_DATA} from '../../constants/subscription/subscription.constants';
import {type Subscription} from '../../models/subscription/subscription.model';

interface EffectsStore {
  setData: (subscriptions: Subscription[]) => void;
}

export function subscriptionsEffects(store: EffectsStore) {
  return {
    load: rxMethod<void>(
      pipe(
        switchMap(() => of(SUBSCRIPTION_MOCK_DATA)),
        tap(subscriptions => store.setData(subscriptions))
      )
    ),
  };
}

interface HookStore extends EffectsStore {
  load: () => void;
}

export function subscriptionsHooks(store: HookStore): void {
  store.load();
}
