/** IBKR self-service consumer keys are exactly 9 alphanumeric characters. */
export const CONSUMER_KEY_LENGTH = 9;

/** The three PEM artifacts uploaded as files and read into hidden form controls. */
export type PemControlName = 'signatureKey' | 'encryptionKey' | 'dhParam';
