import {type SyncStatusResponse} from '../../models/sync/sync.model';

export interface SyncStatusState {
  accountId: string;
  status: Nullable<SyncStatusResponse>;
  isSyncing: boolean;
  errorMessage: Nullable<string>;
}

export const initialSyncStatusState: SyncStatusState = {
  accountId: '',
  status: null,
  isSyncing: false,
  errorMessage: null,
};
