import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type Provider} from '../../models/bank-account/bank-account.model';
import {type InstitutionType, type ModalStep} from '../../models/connect/connect.model';
import {type ConnectState} from './connect.state';

const STEP_FOR_TYPE: Record<InstitutionType, ModalStep> = {
  bank: 'bank-picker',
  crypto: 'binance-form',
  broker: 'ibkr-form',
};

export function connectMethods(store: WritableStateSource<ConnectState>) {
  return {
    openModal(): void {
      patchState(store, {
        modalStep: 'type-picker',
        status: 'idle',
        errorCode: null,
        statusMessage: null,
        institutionType: null,
        selectedProvider: 'plaid',
      });
    },
    closeModal(): void {
      patchState(store, {
        modalStep: 'closed',
        status: 'idle',
        errorCode: null,
        statusMessage: null,
        institutionType: null,
      });
    },
    selectInstitutionType(type: InstitutionType): void {
      patchState(store, {institutionType: type, modalStep: STEP_FOR_TYPE[type], errorCode: null});
    },
    setModalStep(step: ModalStep): void {
      patchState(store, {modalStep: step, errorCode: null});
    },
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
      patchState(store, {
        status: 'success',
        statusMessage: null,
        errorCode: null,
        modalStep: 'closed',
      });
    },
    setError(errorCode: Nullable<string>): void {
      patchState(store, {status: 'error', errorCode, statusMessage: null});
    },
    clearStatus(): void {
      patchState(store, {statusMessage: null});
    },
  };
}
