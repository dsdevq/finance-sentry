import {computed, inject} from '@angular/core';
import {
  patchState,
  signalStoreFeature,
  withComputed,
  withMethods,
  withState,
} from '@ngrx/signals';

import {ErrorMessageService} from '../errors/error-message.service';

export type AsyncStatus = 'idle' | 'loading' | 'success' | 'error';

export interface AsyncStatusState {
  status: AsyncStatus;
  errorCode: string | null;
}

export interface AsyncStatusConfig {
  defaultErrorMessage?: string;
}

const DEFAULT_ERROR_MESSAGE = 'Something went wrong. Please try again.';
const INITIAL_STATE: AsyncStatusState = {status: 'idle', errorCode: null};

export function withAsyncStatus({defaultErrorMessage = DEFAULT_ERROR_MESSAGE}: AsyncStatusConfig = {}) {
  return signalStoreFeature(
    withState(INITIAL_STATE),
    withComputed(store => {
      const errorMessages = inject(ErrorMessageService);
      return {
        isLoading: computed(() => store.status() === 'loading'),
        hasError: computed(() => store.status() === 'error'),
        errorMessage: computed(() => {
          if (store.status() !== 'error') {
            return '';
          }
          return errorMessages.resolve(store.errorCode()) ?? defaultErrorMessage;
        }),
      };
    }),
    withMethods(store => ({
      setLoading(): void {
        patchState(store, {status: 'loading' as AsyncStatus, errorCode: null});
      },
      setSuccess(): void {
        patchState(store, {status: 'success' as AsyncStatus, errorCode: null});
      },
      setError(errorCode: string | null): void {
        patchState(store, {status: 'error' as AsyncStatus, errorCode});
      },
      setIdle(): void {
        patchState(store, INITIAL_STATE);
      },
    }))
  );
}
