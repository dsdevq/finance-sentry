import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {of, pipe, switchMap, tap} from 'rxjs';

import {ALERT_MOCK_DATA} from '../../constants/alert/alert.constants';
import {type Alert} from '../../models/alert/alert.model';

interface EffectsStore {
  setData: (alerts: Alert[]) => void;
}

export function alertsEffects(store: EffectsStore) {
  return {
    load: rxMethod<void>(
      pipe(
        switchMap(() => of(ALERT_MOCK_DATA)),
        tap(alerts => store.setData(alerts))
      )
    ),
  };
}

interface HookStore extends EffectsStore {
  load: () => void;
}

export function alertsHooks(store: HookStore): void {
  store.load();
}
