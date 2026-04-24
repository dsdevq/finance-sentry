import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@dsdevq-common/ui';

import {MS_PER_SECOND} from '../constants/auth.constants';
import {type JwtPayload} from '../models/auth.models';
import {type AuthFlow, type AuthStatus} from './auth.state';

interface StateSignals {
  token: Signal<string | null>;
  status: Signal<AuthStatus>;
  errorCode: Signal<string | null>;
  flow: Signal<AuthFlow>;
}

function isTokenExpired(token: string | null): boolean {
  if (!token) {
    return true;
  }
  try {
    const payload = JSON.parse(atob(token.split('.')[1])) as JwtPayload;
    return payload.exp * MS_PER_SECOND < Date.now();
  } catch {
    return true;
  }
}

function flowFallback(flow: AuthFlow): string {
  return flow === 'login' ? 'Invalid email or password.' : '';
}

export function authComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isAuthenticated: computed(() => {
      const token = store.token();
      return !!token && !isTokenExpired(token);
    }),
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
