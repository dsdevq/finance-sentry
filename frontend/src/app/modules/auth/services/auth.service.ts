import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {Router} from '@angular/router';
import {Observable, tap} from 'rxjs';

import {environment} from '../../../../environments/environment';
import {AppRoute} from '../../../shared/enums/app-route.enum';
import {MS_PER_SECOND, TOKEN_KEY} from '../constants/auth.constants';
import {AuthRequest, AuthResponse, JwtPayload} from '../models/auth.models';

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

  public verifyGoogleCredential(credential: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/google/verify`, {credential})
      .pipe(tap(res => this.storeToken(res.token)));
  }

  public logout(): void {
    this.http
      .post(`${this.apiUrl}/logout`, null, {withCredentials: true})
      .subscribe({error: () => undefined});
    localStorage.removeItem(TOKEN_KEY);
    void this.router.navigate([AppRoute.Login]);
  }
}
