export interface AuthRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  userId: string;
  email: string;
  expiresAt: string;
}

export interface ApiError {
  error: {errorCode?: string};
}
