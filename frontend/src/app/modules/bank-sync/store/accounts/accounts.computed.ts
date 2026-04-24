import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/ui';

import {type BankAccount} from '../../models/bank-account.model';
import {type AccountsStatus} from './accounts.state';

interface StateSignals {
  accounts: Signal<BankAccount[]>;
  status: Signal<AccountsStatus>;
  errorCode: Signal<string | null>;
}

const DEFAULT_ERROR_MESSAGE = 'Failed to load accounts. Please try again.';

export function accountsComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isLoading: computed(() => store.status() === 'loading'),
    isEmpty: computed(() => store.status() === 'idle' && store.accounts().length === 0),
    errorMessage: computed(() => {
      if (store.status() !== 'error') {
        return '';
      }
      return errorMessages.resolve(store.errorCode()) ?? DEFAULT_ERROR_MESSAGE;
    }),
  };
}
