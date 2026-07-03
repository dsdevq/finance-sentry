/**
 * IBKR connect: single blocking POST /brokerage/ibkr/connect that resolves
 * with the initial holdings result on success or a 4xx error body on failure.
 */
export interface ConnectIBKRRequest {
  username: string;
  password: string;
}

export interface IBKRConnectResult {
  accountId: string;
  holdingsCount: number;
  connectedAt: string;
}
