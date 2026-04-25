import {type Provider} from '../../models/bank-account/bank-account.model';

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
  errorCode: Nullable<string>;
  statusMessage: Nullable<string>;
}

export const initialConnectState: ConnectState = {
  selectedProvider: 'plaid',
  status: 'idle',
  errorCode: null,
  statusMessage: null,
};
