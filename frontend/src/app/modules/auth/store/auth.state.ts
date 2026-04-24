import {TOKEN_KEY} from '../constants/auth.constants';

export type AuthStatus = 'idle' | 'loading' | 'error';
export type AuthFlow = 'login' | 'register' | 'google' | null;

export interface FlashMessage {
  kind: 'info' | 'error';
  text: string;
}

export interface AuthState {
  token: string | null;
  userId: string | null;
  expiresAt: string | null;
  status: AuthStatus;
  errorCode: string | null;
  flow: AuthFlow;
  returnUrl: string | null;
  flashMessage: FlashMessage | null;
}

function readToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export const initialAuthState: AuthState = {
  token: readToken(),
  userId: null,
  expiresAt: null,
  status: 'idle',
  errorCode: null,
  flow: null,
  returnUrl: null,
  flashMessage: null,
};
