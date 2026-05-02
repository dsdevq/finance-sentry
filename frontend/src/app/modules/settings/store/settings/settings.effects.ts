import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {of, pipe, switchMap, tap} from 'rxjs';

import {type UserProfile} from '../../models/settings/settings.model';

const MOCK_PROFILE: UserProfile = {
  firstName: 'John',
  lastName: 'Doe',
  email: 'john.doe@example.com',
  baseCurrency: 'USD',
  theme: 'system',
  emailAlerts: true,
  lowBalanceAlerts: true,
  lowBalanceThreshold: 500,
  syncFailureAlerts: true,
  twoFactor: false,
};

interface EffectsStore {
  setProfile: (profile: UserProfile) => void;
  setProfileSaving: (saving: boolean) => void;
  setPasswordSaving: (saving: boolean) => void;
}

export function settingsEffects(store: EffectsStore) {
  return {
    load: rxMethod<void>(
      pipe(
        switchMap(() => of(MOCK_PROFILE)),
        tap(profile => store.setProfile(profile))
      )
    ),
  };
}

interface HookStore extends EffectsStore {
  load: () => void;
}

export function settingsHooks(store: HookStore): void {
  store.load();
}
