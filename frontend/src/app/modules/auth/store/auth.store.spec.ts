import {TestBed} from '@angular/core/testing';
import {Router} from '@angular/router';
import {ERROR_MESSAGES} from '@dsdevq-common/ui';
import {of, Subject, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {ERROR_MESSAGES_REGISTRY} from '../../../core/errors/error-messages.registry';
import {MS_PER_SECOND} from '../constants/auth.constants';
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

function authServiceMock(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    login: vi.fn(),
    register: vi.fn(),
    verifyGoogleCredential: vi.fn(),
    refresh: vi.fn().mockReturnValue(throwError(() => new Error('no cookie'))),
    logout: vi.fn().mockReturnValue(of(null)),
    ...overrides,
  };
}

function configure(authService: unknown, router = routerMock()) {
  TestBed.configureTestingModule({
    providers: [
      {provide: AuthService, useValue: authService},
      {provide: Router, useValue: router},
      {provide: ERROR_MESSAGES, useValue: ERROR_MESSAGES_REGISTRY},
    ],
  });
}

describe('AuthStore (integration)', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('silent refresh on init hydrates token when the refresh cookie is valid', () => {
    const authService = authServiceMock({refresh: vi.fn().mockReturnValue(of(SAMPLE_RESPONSE))});
    configure(authService);

    const store = TestBed.inject(AuthStore);

    expect(authService.refresh).toHaveBeenCalled();
    expect(store.token()).toBe(SAMPLE_RESPONSE.token);
    expect(store.isAuthenticated()).toBe(true);
  });

  it('silent refresh failure on init leaves the store unauthenticated', () => {
    const authService = authServiceMock();
    configure(authService);

    const store = TestBed.inject(AuthStore);

    expect(store.token()).toBeNull();
    expect(store.isAuthenticated()).toBe(false);
  });

  it('successful login stores the token in state', () => {
    const authService = authServiceMock({login: vi.fn().mockReturnValue(of(SAMPLE_RESPONSE))});
    configure(authService);

    const store = TestBed.inject(AuthStore);
    store.login({email: 'a@b.c', password: 'pw'});

    expect(store.token()).toBe(SAMPLE_RESPONSE.token);
    expect(store.isAuthenticated()).toBe(true);
    expect(store.errorMessage()).toBe('');
  });

  it('failed login maps error code through errorMessage computed', () => {
    const authService = authServiceMock({
      login: vi
        .fn()
        .mockReturnValue(throwError(() => ({error: {errorCode: 'GOOGLE_ACCOUNT_ONLY'}}))),
    });
    configure(authService);

    const store = TestBed.inject(AuthStore);
    store.login({email: 'a@b.c', password: 'pw'});

    expect(store.token()).toBeNull();
    expect(store.errorMessage()).toContain('Continue with Google');
  });

  it('logout clears the token in state and navigates', () => {
    const authService = authServiceMock({login: vi.fn().mockReturnValue(of(SAMPLE_RESPONSE))});
    const router = routerMock();
    configure(authService, router);

    const store = TestBed.inject(AuthStore);
    store.login({email: 'a@b.c', password: 'pw'});
    expect(store.token()).toBe(SAMPLE_RESPONSE.token);

    store.logout();

    expect(store.token()).toBeNull();
    expect(router.navigate).toHaveBeenCalled();
  });
});
