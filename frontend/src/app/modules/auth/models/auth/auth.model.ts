export interface AuthRequest {
  email: string;
  password: string;
}

export interface UserDto {
  id: string;
  email: string;
}

export interface AuthResponse {
  user: UserDto;
  expiresAt: string;
}

export interface ApiError {
  error: {errorCode?: string};
}
