/**
 * Per-user IBKR connect: username + password are POSTed and encrypted at rest.
 * The backend then spawns a per-user IBeam gateway container using them.
 */
export interface ConnectIBKRRequest {
  username: string;
  password: string;
}

export interface ConnectIBKRResponse {
  accountId: string;
  holdingsCount: number;
  connectedAt: string;
}
