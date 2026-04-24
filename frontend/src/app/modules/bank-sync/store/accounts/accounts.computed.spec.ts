import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {ERROR_MESSAGES} from '@dsdevq-common/ui';
import {beforeEach, describe, expect, it} from 'vitest';

import {ERROR_MESSAGES_REGISTRY} from '../../../../core/errors/error-messages.registry';
import {type BankAccount} from '../../models/bank-account.model';
import {accountsComputed} from './accounts.computed';
import {type AccountsStatus} from './accounts.state';

function build(
  overrides: Partial<{
    accounts: BankAccount[];
    status: AccountsStatus;
    errorCode: string | null;
  }> = {}
) {
  return {
    accounts: signal<BankAccount[]>(overrides.accounts ?? []),
    status: signal<AccountsStatus>(overrides.status ?? 'idle'),
    errorCode: signal<string | null>(overrides.errorCode ?? null),
  };
}

const FAKE_ACCOUNT = {accountId: 'a1'} as unknown as BankAccount;

describe('accountsComputed', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{provide: ERROR_MESSAGES, useValue: ERROR_MESSAGES_REGISTRY}],
    });
  });

  it('isLoading reflects loading status', () => {
    const store = build({status: 'loading'});
    TestBed.runInInjectionContext(() => {
      expect(accountsComputed(store).isLoading()).toBe(true);
    });
  });

  it('isEmpty is true when idle and no accounts', () => {
    const store = build({status: 'idle', accounts: []});
    TestBed.runInInjectionContext(() => {
      expect(accountsComputed(store).isEmpty()).toBe(true);
    });
  });

  it('isEmpty is false when loading', () => {
    const store = build({status: 'loading', accounts: []});
    TestBed.runInInjectionContext(() => {
      expect(accountsComputed(store).isEmpty()).toBe(false);
    });
  });

  it('isEmpty is false when accounts present', () => {
    const store = build({status: 'idle', accounts: [FAKE_ACCOUNT]});
    TestBed.runInInjectionContext(() => {
      expect(accountsComputed(store).isEmpty()).toBe(false);
    });
  });

  it('errorMessage falls back to default for unknown codes', () => {
    const store = build({status: 'error', errorCode: 'UNKNOWN'});
    TestBed.runInInjectionContext(() => {
      expect(accountsComputed(store).errorMessage()).toContain('Failed to load accounts');
    });
  });

  it('errorMessage resolves known codes via registry', () => {
    const store = build({status: 'error', errorCode: 'MONOBANK_TOKEN_INVALID'});
    TestBed.runInInjectionContext(() => {
      expect(accountsComputed(store).errorMessage()).toContain('Invalid Monobank token');
    });
  });

  it('errorMessage is empty when status is not error', () => {
    const store = build({status: 'idle', errorCode: 'ANY'});
    TestBed.runInInjectionContext(() => {
      expect(accountsComputed(store).errorMessage()).toBe('');
    });
  });
});
