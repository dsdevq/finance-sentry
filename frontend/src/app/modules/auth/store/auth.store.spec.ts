import {TestBed} from '@angular/core/testing';
import {Router} from '@angular/router';
import {ERROR_MESSAGES} from '@dsdevq-common/ui';
import {of, Subject, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {ERROR_MESSAGES_REGISTRY} from '../../../core/errors/error-messages.registry';
import {MS_PER_SECOND, TOKEN_KEY} from '../constants/auth.constants';
import {type AuthResponse} from '../models/auth.models';
import {AuthService} from '../services/auth.service';
import {AuthStore} from './auth.store';

const ONE_HOUR_SECONDS = 3600;

function makeToken(expSecondsFromNow: number): string {
  const header = btoa(JSON.stringify({alg: 'HS256', typ: 'JWT'}));
  const payload = btoa(
    JSON.stringify({exp: Math.floor(Date.now() / MS_PER_SECOND) + expSecondsFromNow})
  );
  return `${header}.${payload}.sig`;
}

const SAMPLE_RESPONSE: AuthResponse = {
  token: makeToken(ONE_HOUR_SECONDS),
  userId: 'u-1',
  expiresAt: '2099-01-01T00:00:00Z',
};

function routerMock(url = '/login') {
  return {
    url,
    events: new Subject<unknown>(),
    navigate: vi.fn(),
    navigateByUrl: vi.fn(),
    routerState: {
      root: {snapshot: {queryParamMap: {get: () => null}}},
    },
  };
}

describe('AuthStore (integration)', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
  });

  it('successful login writes token to state and localStorage', () => {
    const authService = {
      login: vi.fn().mockReturnValue(of(SAMPLE_RESPONSE)),
      register: vi.fn(),
      verifyGoogleCredential: vi.fn(),
      refresh: vi.fn(),
      logout: vi.fn().mockReturnValue(of(null)),
    };
    TestBed.configureTestingModule({
      providers: [
        {provide: AuthService, useValue: authService},
        {provide: Router, useValue: routerMock()},
        {provide: ERROR_MESSAGES, useValue: ERROR_MESSAGES_REGISTRY},
      ],
    });

    const store = TestBed.inject(AuthStore);
    store.login({email: 'a@b.c', password: 'pw'});

    expect(store.token()).toBe(SAMPLE_RESPONSE.token);
    expect(store.isAuthenticated()).toBe(true);
    expect(localStorage.getItem(TOKEN_KEY)).toBe(SAMPLE_RESPONSE.token);
    expect(store.errorMessage()).toBe('');
  });

  it('failed login maps error code through errorMessage computed', () => {
    const authService = {
      login: vi
        .fn()
        .mockReturnValue(throwError(() => ({error: {errorCode: 'GOOGLE_ACCOUNT_ONLY'}}))),
      register: vi.fn(),
      verifyGoogleCredential: vi.fn(),
      refresh: vi.fn(),
      logout: vi.fn().mockReturnValue(of(null)),
    };
    TestBed.configureTestingModule({
      providers: [
        {provide: AuthService, useValue: authService},
        {provide: Router, useValue: routerMock()},
        {provide: ERROR_MESSAGES, useValue: ERROR_MESSAGES_REGISTRY},
      ],
    });

    const store = TestBed.inject(AuthStore);
    store.login({email: 'a@b.c', password: 'pw'});

    expect(store.token()).toBeNull();
    expect(store.errorMessage()).toContain('Continue with Google');
  });

  it('logout clears token in state and localStorage and navigates', () => {
    const authService = {
      login: vi.fn().mockReturnValue(of(SAMPLE_RESPONSE)),
      register: vi.fn(),
      verifyGoogleCredential: vi.fn(),
      refresh: vi.fn(),
      logout: vi.fn().mockReturnValue(of(null)),
    };
    const router = routerMock();
    TestBed.configureTestingModule({
      providers: [
        {provide: AuthService, useValue: authService},
        {provide: Router, useValue: router},
        {provide: ERROR_MESSAGES, useValue: ERROR_MESSAGES_REGISTRY},
      ],
    });

    const store = TestBed.inject(AuthStore);
    store.login({email: 'a@b.c', password: 'pw'});
    expect(localStorage.getItem(TOKEN_KEY)).toBe(SAMPLE_RESPONSE.token);

    store.logout();

    expect(store.token()).toBeNull();
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull();
    expect(router.navigate).toHaveBeenCalled();
  });
});
