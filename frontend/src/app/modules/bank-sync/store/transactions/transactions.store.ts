import {withUrlSync} from '@dsdevq-common/core';
import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {transactionsComputed} from './transactions.computed';
import {transactionsEffects, transactionsHooks} from './transactions.effects';
import {transactionsMethods} from './transactions.methods';
import {initialTransactionsState, type TransactionsState} from './transactions.state';

export const TransactionsStore = signalStore(
  withState(initialTransactionsState),
  withUrlSync<TransactionsState>({
    offset: {param: 'offset', default: 0, codec: 'number'},
    startDate: {param: 'from', default: ''},
    endDate: {param: 'to', default: ''},
  }),
  withMethods(transactionsMethods),
  withComputed(transactionsComputed),
  withMethods(transactionsEffects),
  withHooks(transactionsHooks)
);
