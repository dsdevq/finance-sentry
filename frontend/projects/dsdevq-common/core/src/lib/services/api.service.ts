import {HttpClient, HttpParams} from '@angular/common/http';
import {inject, Injectable} from '@angular/core';
import {type Observable} from 'rxjs';

import {API_BASE_URL} from './api-base-url.token';

export type QueryParams = Record<string, unknown>;

@Injectable()
export abstract class ApiService {
  protected readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);

  // eslint-disable-next-line @angular-eslint/prefer-inject
  protected constructor(private readonly subpath: string) {}

  protected get baseUrl(): string {
    return this.subpath ? `${this.apiBaseUrl}/${this.subpath}` : this.apiBaseUrl;
  }

  protected get<T>(path = '', params?: object): Observable<T> {
    return this.http.get<T>(this.url(path), {params: this.buildParams(params)});
  }

  protected post<T>(path = '', body: unknown = null, params?: object): Observable<T> {
    return this.http.post<T>(this.url(path), body, {params: this.buildParams(params)});
  }

  protected put<T>(path = '', body: unknown = null, params?: object): Observable<T> {
    return this.http.put<T>(this.url(path), body, {params: this.buildParams(params)});
  }

  protected patch<T>(path = '', body: unknown = null, params?: object): Observable<T> {
    return this.http.patch<T>(this.url(path), body, {params: this.buildParams(params)});
  }

  protected delete<T>(path = '', params?: object): Observable<T> {
    return this.http.delete<T>(this.url(path), {params: this.buildParams(params)});
  }

  protected buildParams(input?: object): HttpParams {
    let params = new HttpParams();
    if (!input) {
      return params;
    }
    for (const [key, value] of Object.entries(input as QueryParams)) {
      if (this.isSerializableParam(value)) {
        params = params.set(key, String(value));
      }
    }
    return params;
  }

  private isSerializableParam(value: unknown): value is string | number | boolean {
    if (value === null || value === undefined || value === '') {
      return false;
    }
    return typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean';
  }

  private url(path: string): string {
    return path ? `${this.baseUrl}/${path}` : this.baseUrl;
  }
}
