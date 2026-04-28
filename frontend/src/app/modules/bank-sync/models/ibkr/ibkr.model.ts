/**
 * Single-tenant IBKR connect: the gateway sidecar (IBeam) holds the session,
 * so the connect endpoint takes no body. The frontend ignores the response
 * payload — the success outcome is signalled by the 201 Created status.
 */
export interface ConnectIBKRResponse {
  accountId: string;
  holdingsCount: number;
  connectedAt: string;
}
