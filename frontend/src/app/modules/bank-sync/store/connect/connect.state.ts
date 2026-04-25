import {type Provider} from '../../models/bank-account/bank-account.model';
import {type InstitutionType, type ModalStep} from '../../models/connect/connect.model';

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
  modalStep: ModalStep;
  institutionType: Nullable<InstitutionType>;
}

export const initialConnectState: ConnectState = {
  selectedProvider: 'plaid',
  status: 'idle',
  errorCode: null,
  statusMessage: null,
  modalStep: 'closed',
  institutionType: null,
};
