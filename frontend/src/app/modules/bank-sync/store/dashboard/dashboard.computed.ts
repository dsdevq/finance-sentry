import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/ui';

import {type DashboardData} from '../../models/dashboard.model';
import {type DashboardStatus} from './dashboard.state';

interface StateSignals {
  data: Signal<DashboardData | null>;
  status: Signal<DashboardStatus>;
  errorCode: Signal<string | null>;
}

const DEFAULT_ERROR_MESSAGE = 'Failed to load dashboard data. Please try again.';

export function dashboardComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isLoading: computed(() => store.status() === 'loading'),
    errorMessage: computed(() => {
      if (store.status() !== 'error') {
        return '';
      }
      return errorMessages.resolve(store.errorCode()) ?? DEFAULT_ERROR_MESSAGE;
    }),
    currencyEntries: computed(() => Object.entries(store.data()?.aggregatedBalance ?? {})),
    accountTypeEntries: computed(() => Object.entries(store.data()?.accountsByType ?? {})),
  };
}
