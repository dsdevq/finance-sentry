import {withAsyncStatus, withPagination, withUrlSync} from '@dsdevq-common/core';
import {signalStore, withHooks, withMethods, withState} from '@ngrx/signals';

import {transactionsEffects, transactionsHooks} from './transactions.effects';
import {transactionsMethods} from './transactions.methods';
import {initialTransactionsState, PAGE_SIZE} from './transactions.state';

export const TransactionsStore = signalStore(
  withState(initialTransactionsState),
  withAsyncStatus({defaultErrorMessage: 'Failed to load transactions. Please try again.'}),
  withPagination(PAGE_SIZE),
  withUrlSync({
    offset: {param: 'offset', default: 0, codec: 'number'},
    startDate: {param: 'from', default: ''},
    endDate: {param: 'to', default: ''},
  }),
  withMethods(transactionsMethods),
  withMethods(transactionsEffects),
  withHooks(transactionsHooks)
);
