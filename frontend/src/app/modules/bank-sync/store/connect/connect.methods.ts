import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type Provider} from '../../models/bank-account/bank-account.model';
import {type ConnectState} from './connect.state';

export function connectMethods(store: WritableStateSource<ConnectState>) {
  return {
    selectProvider(provider: Provider): void {
      patchState(store, {selectedProvider: provider, errorCode: null, statusMessage: null});
    },
    setInitializing(): void {
      patchState(store, {status: 'initializing', errorCode: null, statusMessage: null});
    },
    setReady(): void {
      patchState(store, {status: 'ready', errorCode: null});
    },
    setSyncing(message: string): void {
      patchState(store, {status: 'syncing', statusMessage: message, errorCode: null});
    },
    setPolling(message: string): void {
      patchState(store, {status: 'polling', statusMessage: message});
    },
    setSuccess(): void {
      patchState(store, {status: 'success', statusMessage: null, errorCode: null});
    },
    setError(errorCode: Nullable<string>): void {
      patchState(store, {status: 'error', errorCode, statusMessage: null});
    },
    clearStatus(): void {
      patchState(store, {statusMessage: null});
    },
  };
}
