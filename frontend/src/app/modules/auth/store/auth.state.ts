export type AuthStatus = 'idle' | 'loading' | 'error';
export type AuthFlow = 'login' | 'register' | 'google' | null;

export interface FlashMessage {
  kind: 'info' | 'error';
  text: string;
}

export interface AuthState {
  userId: string | null;
  email: string | null;
  status: AuthStatus;
  errorCode: string | null;
  flow: AuthFlow;
  returnUrl: string | null;
  flashMessage: FlashMessage | null;
}

export const initialAuthState: AuthState = {
  userId: null,
  email: null,
  status: 'idle',
  errorCode: null,
  flow: null,
  returnUrl: null,
  flashMessage: null,
};
