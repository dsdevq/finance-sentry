import {type Provider} from '../../models/bank-account.model';

export type ConnectStatus =
  | 'idle'
  | 'initializing'
  | 'ready'
  | 'syncing'
  | 'polling'
  | 'error'
  | 'success';

export interface ConnectState {
  selectedProvider: Provider;
  status: ConnectStatus;
  errorCode: string | null;
  statusMessage: string | null;
}

export const initialConnectState: ConnectState = {
  selectedProvider: 'plaid',
  status: 'idle',
  errorCode: null,
  statusMessage: null,
};
