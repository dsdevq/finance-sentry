/**
 * IBKR connect via OAuth 1.0a self-service. The user registers their own
 * consumer key + access token on IBKR's self-service OAuth portal and generates
 * three PEM artifacts locally; all six values are posted to
 * POST /brokerage/ibkr/connect. The backend stores the secret material encrypted
 * at rest (AES-256-GCM) and signs every IBKR request — no IBeam session, no 2FA.
 */
export interface ConnectIBKRRequest {
  consumerKey: string;
  accessToken: string;
  accessTokenSecret: string;
  /** Contents of private_signature.pem — RSA key that signs the live-session-token request. */
  signatureKey: string;
  /** Contents of private_encryption.pem — RSA key that decrypts the access token secret. */
  encryptionKey: string;
  /** Contents of dhparam.pem — Diffie-Hellman parameters seeding the live-session-token exchange. */
  dhParam: string;
}

export interface IBKRConnectResult {
  accountId: string;
  holdingsCount: number;
  connectedAt: string;
}
