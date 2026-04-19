export interface AuthRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  userId: string;
}

export interface JwtPayload {
  exp: number;
}

export interface ApiError {
  error: {errorCode?: string};
}
