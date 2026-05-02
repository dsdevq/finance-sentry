import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/core';

import {type Provider} from '../../../../shared/models/provider/provider.model';
import {type ModalStep} from '../../models/connect/connect.model';
import {AccountsStore} from '../accounts/accounts.store';
import {type ConnectStatus} from './connect.state';

interface StateSignals {
  selectedProvider: Signal<Provider>;
  status: Signal<ConnectStatus>;
  errorCode: Signal<Nullable<string>>;
  statusMessage: Signal<Nullable<string>>;
  modalStep: Signal<ModalStep>;
}

const DEFAULT_BINANCE_ERROR = 'Failed to connect Binance account. Please check your API keys.';
const DEFAULT_IBKR_ERROR = 'Failed to connect IBKR account. Please check your credentials.';
const DEFAULT_MONOBANK_ERROR = 'Failed to connect Monobank account. Please try again.';
const PLAID_INIT_ERROR = 'Failed to initialize bank connection. Please try again.';
const PLAID_LINK_ERROR = 'Failed to link account. Please try again.';

function mapErrorByProvider(
  code: Nullable<string>,
  provider: Provider,
  resolved: Nullable<string>
): string {
  if (resolved) {
    return resolved;
  }
  switch (provider) {
    case 'plaid':
      return code === 'PLAID_LINK_FAILED' ? PLAID_LINK_ERROR : PLAID_INIT_ERROR;
    case 'binance':
      return DEFAULT_BINANCE_ERROR;
    case 'ibkr':
      return DEFAULT_IBKR_ERROR;
    default:
      return DEFAULT_MONOBANK_ERROR;
  }
}

const PROVIDER_SLUGS: readonly Provider[] = ['plaid', 'monobank', 'binance', 'ibkr'];

export function connectComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);
  const accountsStore = inject(AccountsStore, {optional: true});

  return {
    isModalOpen: computed(() => store.modalStep() !== 'closed'),
    isBusy: computed(() => {
      const s = store.status();
      return s === 'initializing' || s === 'syncing' || s === 'polling';
    }),
    isInitializing: computed(() => store.status() === 'initializing'),
    isPlaidReady: computed(() => store.status() === 'ready'),
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
    connectedProviders: computed<ReadonlySet<Provider>>(() => {
      const summary = accountsStore?.summary();
      if (!summary) {
        return new Set();
      }
      const known = new Set(PROVIDER_SLUGS as readonly string[]);
      const set = new Set<Provider>();
      for (const category of summary.categories) {
        for (const account of category.accounts) {
          if (known.has(account.provider)) {
            set.add(account.provider as Provider);
          }
        }
      }
      return set;
    }),
  };
}
