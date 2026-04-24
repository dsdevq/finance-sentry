import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {authComputed} from './auth.computed';
import {authEffects, authHooks} from './auth.effects';
import {authMethods} from './auth.methods';
import {initialAuthState} from './auth.state';

export const AuthStore = signalStore(
  {providedIn: 'root'},
  withState(initialAuthState),
  withMethods(authMethods),
  withComputed(authComputed),
  withMethods(authEffects),
  withHooks({onInit: authHooks})
);
