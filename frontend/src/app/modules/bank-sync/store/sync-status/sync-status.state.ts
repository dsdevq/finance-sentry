import {type SyncStatusResponse} from '../../models/sync.model';

export interface SyncStatusState {
  accountId: string;
  status: SyncStatusResponse | null;
  isSyncing: boolean;
  errorMessage: string | null;
}

export const initialSyncStatusState: SyncStatusState = {
  accountId: '',
  status: null,
  isSyncing: false,
  errorMessage: null,
};
