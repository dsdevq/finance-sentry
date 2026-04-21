export interface SyncStatusResponse {
  status: 'pending' | 'running' | 'success' | 'failed';
  transactionCountFetched: number;
  transactionCountDeduped: number;
  errorMessage: string | null;
  lastSyncTimestamp: string | null;
  startedAt: string | null;
  completedAt: string | null;
}

export interface TriggerSyncResponse {
  jobId: string;
  message: string;
}
