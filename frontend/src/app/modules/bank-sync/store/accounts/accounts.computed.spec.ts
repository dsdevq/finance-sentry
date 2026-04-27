import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {ERROR_MESSAGES} from '@dsdevq-common/ui';
import {beforeEach, describe, expect, it} from 'vitest';

import {ERROR_MESSAGES_REGISTRY} from '../../../../core/errors/error-messages.registry';
import {
  type AccountBalanceItem,
  type WealthSummaryResponse,
} from '../../../../shared/models/wealth/wealth.model';
import {accountsComputed} from './accounts.computed';

const FAKE_ACCOUNT: AccountBalanceItem = {
  accountId: 'a1',
  bankName: 'Test Bank',
  accountType: 'checking',
  accountNumberLast4: '1234',
  provider: 'plaid',
  category: 'banking',
  currency: 'USD',
  currentBalance: 1000,
  balanceInBaseCurrency: 1000,
  syncStatus: 'synced',
  lastSyncTimestamp: null,
};

const FAKE_SUMMARY: WealthSummaryResponse = {
  totalNetWorth: 1000,
  baseCurrency: 'USD',
  categories: [{category: 'banking', totalInBaseCurrency: 1000, accounts: [FAKE_ACCOUNT]}],
  appliedFilters: {category: null, provider: null},
};

function build(
  overrides: Partial<{
    summary: Nullable<WealthSummaryResponse>;
    status: AsyncStatus;
    errorCode: Nullable<string>;
  }> = {}
) {
  return {
    summary: signal<Nullable<WealthSummaryResponse>>(overrides.summary ?? null),
    status: signal<AsyncStatus>(overrides.status ?? 'idle'),
    errorCode: signal<Nullable<string>>(overrides.errorCode ?? null),
  };
}

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

  it('isEmpty is true when idle and no summary', () => {
    const store = build({status: 'idle', summary: null});
    TestBed.runInInjectionContext(() => {
      expect(accountsComputed(store).isEmpty()).toBe(true);
    });
  });

  it('isEmpty is false when loading', () => {
    const store = build({status: 'loading', summary: null});
    TestBed.runInInjectionContext(() => {
      expect(accountsComputed(store).isEmpty()).toBe(false);
    });
  });

  it('isEmpty is false when summary has categories', () => {
    const store = build({status: 'idle', summary: FAKE_SUMMARY});
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

  it('errorMessage is empty when status is not error', () => {
    const store = build({status: 'idle', errorCode: 'ANY'});
    TestBed.runInInjectionContext(() => {
      expect(accountsComputed(store).errorMessage()).toBe('');
    });
  });

  it('totalNetWorth returns 0 when no summary', () => {
    const store = build({summary: null});
    TestBed.runInInjectionContext(() => {
      expect(accountsComputed(store).totalNetWorth()).toBe(0);
    });
  });

  it('totalNetWorth returns summary value', () => {
    const store = build({summary: FAKE_SUMMARY});
    TestBed.runInInjectionContext(() => {
      expect(accountsComputed(store).totalNetWorth()).toBe(1000);
    });
  });
});
