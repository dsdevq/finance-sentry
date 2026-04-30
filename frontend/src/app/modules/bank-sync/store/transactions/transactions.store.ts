import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {transactionsComputed} from './transactions.computed';
import {transactionsEffects, transactionsHooks} from './transactions.effects';
import {transactionsMethods} from './transactions.methods';
import {initialTransactionsState} from './transactions.state';

export const TransactionsStore = signalStore(
  withState(initialTransactionsState),
  withMethods(transactionsMethods),
  withComputed(transactionsComputed),
  withMethods(transactionsEffects),
  withHooks(transactionsHooks)
);
