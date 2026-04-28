import {type ErrorMessagesMap} from '@dsdevq-common/ui';

export const ERROR_MESSAGES_REGISTRY: ErrorMessagesMap = {
  GOOGLE_ACCOUNT_ONLY: "This account uses Google sign-in. Click 'Continue with Google' instead.",
  DUPLICATE_EMAIL: 'Email is already registered.',
  MONOBANK_TOKEN_INVALID: 'Invalid Monobank token. Please check and try again.',
  MONOBANK_TOKEN_DUPLICATE: 'This Monobank token is already connected.',
  MONOBANK_RATE_LIMITED: 'Monobank rate limit reached. Please wait 60 seconds and try again.',
  BINANCE_INVALID_CREDENTIALS:
    'Binance rejected the provided credentials. Use a read-only API key with no IP restrictions.',
  BINANCE_DUPLICATE:
    'Binance account already connected. Disconnect the existing one to use new keys.',
  IBKR_INVALID_CREDENTIALS:
    'IB Gateway rejected the provided credentials. Confirm the 2FA push notification on your phone and try again.',
  IBKR_DUPLICATE: 'IBKR account already connected. Disconnect the existing one to reconnect.',
  PLAID_DUPLICATE: 'This bank is already connected. View it in your accounts list.',
  PLAID_SCRIPT_LOAD_FAILED: 'Plaid is unavailable. Disable any ad blocker and refresh the page.',
  VALIDATION_ERROR: 'Some fields look wrong — please review the highlighted errors.',
};
