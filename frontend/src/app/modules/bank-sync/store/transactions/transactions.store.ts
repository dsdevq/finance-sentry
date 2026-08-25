import {withAsyncStatus, withPagination, withUrlSync} from '@lifekit-hq/core';
import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {transactionsComputed} from './transactions.computed';
import {transactionsEffects, transactionsHooks} from './transactions.effects';
import {transactionsMethods} from './transactions.methods';
import {initialTransactionsState, PAGE_SIZE} from './transactions.state';

export const TransactionsStore = signalStore(
  withState(initialTransactionsState),
  withAsyncStatus({defaultErrorMessage: 'Failed to load transactions. Please try again.'}),
  withPagination(PAGE_SIZE),
  withUrlSync({
    offset: {param: 'offset', default: 0, codec: 'number'},
  }),
  withMethods(transactionsMethods),
  withComputed(transactionsComputed),
  withMethods(transactionsEffects),
  withHooks(transactionsHooks)
);
