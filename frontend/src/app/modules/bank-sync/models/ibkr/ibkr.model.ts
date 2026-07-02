/**
 * Per-user IBKR async connect. The frontend POSTs credentials, gets back a
 * sessionId, then polls GET /brokerage/ibkr/connect/{sessionId} until it
 * observes a terminal state (`completed`, `failed`, `cancelled`).
 */
export interface ConnectIBKRRequest {
  username: string;
  password: string;
}

export interface ConnectIBKRSessionAccepted {
  sessionId: string;
}

export type IBKRConnectStatus =
  | 'pending'
  | 'spawning'
  | 'awaitingAuth'
  | 'syncing'
  | 'completed'
  | 'failed'
  | 'cancelled';

export interface IBKRConnectResult {
  accountId: string;
  holdingsCount: number;
  connectedAt: string;
}

export interface IBKRConnectSessionSnapshot {
  sessionId: string;
  status: IBKRConnectStatus;
  errorCode: string | null;
  errorMessage: string | null;
  result: IBKRConnectResult | null;
  createdAt: string;
  updatedAt: string;
}
