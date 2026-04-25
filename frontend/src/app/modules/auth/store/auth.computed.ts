import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/ui';

import {type AuthFlow, type AuthStatus} from './auth.state';

interface StateSignals {
  userId: Signal<string | null>;
  status: Signal<AuthStatus>;
  errorCode: Signal<string | null>;
  flow: Signal<AuthFlow>;
}

function flowFallback(flow: AuthFlow): string {
  return flow === 'login' ? 'Invalid email or password.' : '';
}

export function authComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isAuthenticated: computed(() => store.userId() !== null),
    isLoading: computed(() => store.status() === 'loading'),
    errorMessage: computed(() => {
      const code = store.errorCode();
      if (!code) {
        return '';
      }
      return errorMessages.resolve(code) ?? flowFallback(store.flow());
    }),
  };
}
