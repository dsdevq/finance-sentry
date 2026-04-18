import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {Router} from '@angular/router';
import {Observable, tap} from 'rxjs';

import {environment} from '../../../../environments/environment';
import {AuthRequest, AuthResponse} from '../models/auth.models';

const TOKEN_KEY = 'fs_auth_token';
const MS_PER_SECOND = 1000;

interface JwtPayload {
  exp: number;
}

@Injectable({providedIn: 'root'})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = `${environment.apiBaseUrl}/auth`;

  public login(req: AuthRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, req)
      .pipe(tap(res => this.storeToken(res.token)));
  }

  public register(req: AuthRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/register`, req)
      .pipe(tap(res => this.storeToken(res.token)));
  }

  public refresh(): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/refresh`, null, {withCredentials: true})
      .pipe(tap(res => this.storeToken(res.token)));
  }

  public storeToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  }

  public getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  public isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }
    return !this.isTokenExpired();
  }

  public isTokenExpired(): boolean {
    const token = this.getToken();
    if (!token) {
      return true;
    }

    try {
      const payload = JSON.parse(atob(token.split('.')[1])) as JwtPayload;
      return payload.exp * MS_PER_SECOND < Date.now();
    } catch {
      return true;
    }
  }

  public googleLogin(): void {
    window.location.href = '/api/v1/auth/google/login';
  }

  public handleOAuthCallback(token: string, userId: string, expiresAt: string): void {
    this.storeToken(token);
    localStorage.setItem('fs_user_id', userId);
    localStorage.setItem('fs_token_expires_at', expiresAt);
    void this.router.navigate(['/accounts']);
  }

  public logout(): void {
    this.http
      .post(`${this.apiUrl}/logout`, null, {withCredentials: true})
      .subscribe({error: () => undefined});
    localStorage.removeItem(TOKEN_KEY);
    void this.router.navigate(['/login']);
  }
}
