import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {ERROR_MESSAGES} from '@dsdevq-common/ui';
import {beforeEach, describe, expect, it} from 'vitest';

import {ERROR_MESSAGES_REGISTRY} from '../../../../core/errors/error-messages.registry';
import {type DashboardData} from '../../models/dashboard/dashboard.model';
import {dashboardComputed} from './dashboard.computed';

const EMPTY_DATA: DashboardData = {
  aggregatedBalance: {},
  accountCount: 0,
  accountsByType: {},
  monthlyFlow: [],
  topCategories: [],
  lastSyncTimestamp: null,
};

function build(
  overrides: Partial<{
    data: Nullable<DashboardData>;
    status: AsyncStatus;
    errorCode: Nullable<string>;
  }> = {}
) {
  return {
    data: signal<Nullable<DashboardData>>(overrides.data ?? null),
    status: signal<AsyncStatus>(overrides.status ?? 'idle'),
    errorCode: signal<Nullable<string>>(overrides.errorCode ?? null),
  };
}

describe('dashboardComputed', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{provide: ERROR_MESSAGES, useValue: ERROR_MESSAGES_REGISTRY}],
    });
  });

  it('isLoading reflects loading status', () => {
    const store = build({status: 'loading'});
    TestBed.runInInjectionContext(() => {
      expect(dashboardComputed(store).isLoading()).toBe(true);
    });
  });

  it('errorMessage is empty when status is not error', () => {
    const store = build({status: 'idle', errorCode: 'ANY'});
    TestBed.runInInjectionContext(() => {
      expect(dashboardComputed(store).errorMessage()).toBe('');
    });
  });

  it('errorMessage falls back to default when errorCode is unknown', () => {
    const store = build({status: 'error', errorCode: 'UNKNOWN_CODE'});
    TestBed.runInInjectionContext(() => {
      expect(dashboardComputed(store).errorMessage()).toContain('Failed to load');
    });
  });

  it('errorMessage resolves known codes from registry', () => {
    const store = build({status: 'error', errorCode: 'GOOGLE_ACCOUNT_ONLY'});
    TestBed.runInInjectionContext(() => {
      expect(dashboardComputed(store).errorMessage()).toContain('Continue with Google');
    });
  });

  it('currencyEntries derives from aggregatedBalance', () => {
    const store = build({data: {...EMPTY_DATA, aggregatedBalance: {USD: 100, EUR: 50}}});
    TestBed.runInInjectionContext(() => {
      expect(dashboardComputed(store).currencyEntries()).toEqual([
        ['USD', 100],
        ['EUR', 50],
      ]);
    });
  });

  it('accountTypeEntries derives from accountsByType', () => {
    const store = build({data: {...EMPTY_DATA, accountsByType: {checking: 2, savings: 1}}});
    TestBed.runInInjectionContext(() => {
      expect(dashboardComputed(store).accountTypeEntries()).toEqual([
        ['checking', 2],
        ['savings', 1],
      ]);
    });
  });

  it('entries are empty when data is null', () => {
    const store = build({data: null});
    TestBed.runInInjectionContext(() => {
      const c = dashboardComputed(store);
      expect(c.currencyEntries()).toEqual([]);
      expect(c.accountTypeEntries()).toEqual([]);
    });
  });
});
