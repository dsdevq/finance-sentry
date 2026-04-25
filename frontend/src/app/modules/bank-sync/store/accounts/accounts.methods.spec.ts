import {signalState} from '@ngrx/signals';
import {describe, expect, it} from 'vitest';

import {type WealthSummaryResponse} from '../../models/wealth.model';
import {accountsMethods} from './accounts.methods';
import {initialAccountsState} from './accounts.state';

const FAKE_SUMMARY: WealthSummaryResponse = {
  totalNetWorth: 500,
  baseCurrency: 'USD',
  categories: [],
  appliedFilters: {category: null, provider: null},
};

describe('accountsMethods', () => {
  it('setLoading clears error and flips status', () => {
    const state = signalState(initialAccountsState);
    const methods = accountsMethods(state);
    methods.setError('X');

    methods.setLoading();

    expect(state.status()).toBe('loading');
    expect(state.errorCode()).toBeNull();
  });

  it('setSummary stores the summary and returns to idle', () => {
    const state = signalState(initialAccountsState);
    const methods = accountsMethods(state);

    methods.setSummary(FAKE_SUMMARY);

    expect(state.summary()).toEqual(FAKE_SUMMARY);
    expect(state.status()).toBe('idle');
  });

  it('setError flags error status with code', () => {
    const state = signalState(initialAccountsState);
    const methods = accountsMethods(state);

    methods.setError('BOOM');

    expect(state.status()).toBe('error');
    expect(state.errorCode()).toBe('BOOM');
  });
});
