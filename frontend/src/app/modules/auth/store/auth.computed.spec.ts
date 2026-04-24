import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {ERROR_MESSAGES} from '@dsdevq-common/ui';
import {beforeEach, describe, expect, it} from 'vitest';

import {ERROR_MESSAGES_REGISTRY} from '../../../core/errors/error-messages.registry';
import {MS_PER_SECOND} from '../constants/auth.constants';
import {authComputed} from './auth.computed';
import {type AuthFlow, type AuthStatus} from './auth.state';

function makeToken(expSecondsFromNow: number): string {
  const header = btoa(JSON.stringify({alg: 'HS256', typ: 'JWT'}));
  const payload = btoa(
    JSON.stringify({exp: Math.floor(Date.now() / MS_PER_SECOND) + expSecondsFromNow})
  );
  return `${header}.${payload}.sig`;
}

const ONE_HOUR_SECONDS = 3600;
const ONE_HOUR_AGO_SECONDS = -3600;

function build(
  overrides: Partial<{
    token: string | null;
    status: AuthStatus;
    errorCode: string | null;
    flow: AuthFlow;
  }> = {}
) {
  return {
    token: signal<string | null>(overrides.token ?? null),
    status: signal<AuthStatus>(overrides.status ?? 'idle'),
    errorCode: signal<string | null>(overrides.errorCode ?? null),
    flow: signal<AuthFlow>(overrides.flow ?? null),
  };
}

describe('authComputed', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{provide: ERROR_MESSAGES, useValue: ERROR_MESSAGES_REGISTRY}],
    });
  });

  describe('isAuthenticated', () => {
    it('returns true for a non-expired token', () => {
      const store = build({token: makeToken(ONE_HOUR_SECONDS)});
      TestBed.runInInjectionContext(() => {
        const {isAuthenticated} = authComputed(store);
        expect(isAuthenticated()).toBe(true);
      });
    });

    it('returns false when token is null', () => {
      const store = build({token: null});
      TestBed.runInInjectionContext(() => {
        const {isAuthenticated} = authComputed(store);
        expect(isAuthenticated()).toBe(false);
      });
    });

    it('returns false when token is expired', () => {
      const store = build({token: makeToken(ONE_HOUR_AGO_SECONDS)});
      TestBed.runInInjectionContext(() => {
        const {isAuthenticated} = authComputed(store);
        expect(isAuthenticated()).toBe(false);
      });
    });

    it('returns false when token is malformed', () => {
      const store = build({token: 'not-a-jwt'});
      TestBed.runInInjectionContext(() => {
        const {isAuthenticated} = authComputed(store);
        expect(isAuthenticated()).toBe(false);
      });
    });
  });

  describe('isLoading', () => {
    it('is true when status is loading', () => {
      const store = build({status: 'loading'});
      TestBed.runInInjectionContext(() => {
        expect(authComputed(store).isLoading()).toBe(true);
      });
    });

    it('is false otherwise', () => {
      const store = build({status: 'idle'});
      TestBed.runInInjectionContext(() => {
        expect(authComputed(store).isLoading()).toBe(false);
      });
    });
  });

  describe('errorMessage', () => {
    it('maps GOOGLE_ACCOUNT_ONLY to the google prompt', () => {
      const store = build({errorCode: 'GOOGLE_ACCOUNT_ONLY', flow: 'login'});
      TestBed.runInInjectionContext(() => {
        expect(authComputed(store).errorMessage()).toContain('Continue with Google');
      });
    });

    it('maps DUPLICATE_EMAIL regardless of flow', () => {
      const store = build({errorCode: 'DUPLICATE_EMAIL', flow: 'register'});
      TestBed.runInInjectionContext(() => {
        expect(authComputed(store).errorMessage()).toBe('Email is already registered.');
      });
    });

    it('returns generic invalid-credentials message for unknown code on login flow', () => {
      const store = build({errorCode: 'SOME_OTHER_CODE', flow: 'login'});
      TestBed.runInInjectionContext(() => {
        expect(authComputed(store).errorMessage()).toBe('Invalid email or password.');
      });
    });

    it('returns empty string for unknown code on register flow', () => {
      const store = build({errorCode: 'SOME_OTHER_CODE', flow: 'register'});
      TestBed.runInInjectionContext(() => {
        expect(authComputed(store).errorMessage()).toBe('');
      });
    });

    it('returns empty string when errorCode is null', () => {
      const store = build({errorCode: null, flow: 'login'});
      TestBed.runInInjectionContext(() => {
        expect(authComputed(store).errorMessage()).toBe('');
      });
    });
  });
});
