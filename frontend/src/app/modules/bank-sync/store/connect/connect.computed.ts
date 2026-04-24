import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/ui';

import {type Provider} from '../../models/bank-account.model';
import {type ConnectStatus} from './connect.state';

interface StateSignals {
  selectedProvider: Signal<Provider>;
  status: Signal<ConnectStatus>;
  errorCode: Signal<string | null>;
  statusMessage: Signal<string | null>;
}

const DEFAULT_MONOBANK_ERROR = 'Failed to connect Monobank account. Please try again.';
const PLAID_INIT_ERROR = 'Failed to initialize bank connection. Please try again.';
const PLAID_LINK_ERROR = 'Failed to link account. Please try again.';

function mapErrorByProvider(
  code: string | null,
  provider: Provider,
  resolved: string | null
): string {
  if (resolved) {
    return resolved;
  }
  if (provider === 'plaid') {
    return code === 'PLAID_LINK_FAILED' ? PLAID_LINK_ERROR : PLAID_INIT_ERROR;
  }
  return DEFAULT_MONOBANK_ERROR;
}

export function connectComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isBusy: computed(() => {
      const s = store.status();
      return s === 'initializing' || s === 'syncing' || s === 'polling';
    }),
    isInitializing: computed(() => store.status() === 'initializing'),
    isPlaidReady: computed(() => store.status() === 'ready' || store.status() === 'idle'),
    errorMessage: computed(() => {
      if (store.status() !== 'error') {
        return '';
      }
      return mapErrorByProvider(
        store.errorCode(),
        store.selectedProvider(),
        errorMessages.resolve(store.errorCode())
      );
    }),
  };
}
