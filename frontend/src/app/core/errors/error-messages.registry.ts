import {type ErrorMessagesMap} from '@dsdevq-common/ui';

export const ERROR_MESSAGES_REGISTRY: ErrorMessagesMap = {
  GOOGLE_ACCOUNT_ONLY: "This account uses Google sign-in. Click 'Continue with Google' instead.",
  DUPLICATE_EMAIL: 'Email is already registered.',
  MONOBANK_TOKEN_INVALID: 'Invalid Monobank token. Please check and try again.',
  MONOBANK_TOKEN_DUPLICATE: 'This Monobank token is already connected.',
  MONOBANK_RATE_LIMITED: 'Monobank rate limit reached. Please wait 60 seconds and try again.',
};
