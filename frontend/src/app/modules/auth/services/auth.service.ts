import {HttpClient} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {Observable} from 'rxjs';

import {environment} from '../../../../environments/environment';
import {AuthRequest, AuthResponse} from '../models/auth.models';

const WITH_CREDENTIALS = {withCredentials: true} as const;

@Injectable({providedIn: 'root'})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/auth`;

  public login(req: AuthRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, req, WITH_CREDENTIALS);
  }

  public register(req: AuthRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, req, WITH_CREDENTIALS);
  }

  public refresh(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, null, WITH_CREDENTIALS);
  }

  public verifyGoogleCredential(credential: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${this.apiUrl}/google/verify`,
      {credential},
      WITH_CREDENTIALS
    );
  }

  public logout(): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/logout`, null, WITH_CREDENTIALS);
  }
}
