import {patchState, type WritableStateSource} from '@ngrx/signals';

import {TOKEN_KEY} from '../constants/auth.constants';
import {type AuthResponse} from '../models/auth.models';
import {type AuthFlow, type AuthState, type FlashMessage} from './auth.state';

function writeToken(token: string | null): void {
  if (token) {
    localStorage.setItem(TOKEN_KEY, token);
  } else {
    localStorage.removeItem(TOKEN_KEY);
  }
}

export function authMethods(store: WritableStateSource<AuthState>) {
  return {
    applyAuthResponse(res: AuthResponse): void {
      writeToken(res.token);
      patchState(store, {
        token: res.token,
        userId: res.userId,
        expiresAt: res.expiresAt,
        status: 'idle',
        errorCode: null,
        flow: null,
        flashMessage: null,
      });
    },
    clearSession(): void {
      writeToken(null);
      patchState(store, {
        token: null,
        userId: null,
        expiresAt: null,
        status: 'idle',
        errorCode: null,
        flow: null,
      });
    },
    setLoading(flow: AuthFlow): void {
      patchState(store, {status: 'loading', errorCode: null, flow});
    },
    setError(errorCode: string | null, flow: AuthFlow): void {
      patchState(store, {status: 'error', errorCode, flow});
    },
    resetError(): void {
      patchState(store, {status: 'idle', errorCode: null});
    },
    setReturnUrl(returnUrl: string | null): void {
      patchState(store, {returnUrl});
    },
    setFlashMessage(flashMessage: FlashMessage | null): void {
      patchState(store, {flashMessage});
    },
  };
}
