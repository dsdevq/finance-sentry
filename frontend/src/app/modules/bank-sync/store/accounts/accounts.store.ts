import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {accountsComputed} from './accounts.computed';
import {accountsEffects, accountsHooks} from './accounts.effects';
import {accountsMethods} from './accounts.methods';
import {initialAccountsState} from './accounts.state';

export const AccountsStore = signalStore(
  withState(initialAccountsState),
  withMethods(accountsMethods),
  withComputed(accountsComputed),
  withMethods(accountsEffects),
  withHooks({onInit: accountsHooks})
);
