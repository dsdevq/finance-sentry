import {signalState} from '@ngrx/signals';
import {describe, expect, it} from 'vitest';

import {type DashboardData} from '../../models/dashboard.model';
import {dashboardMethods} from './dashboard.methods';
import {initialDashboardState} from './dashboard.state';

const SAMPLE_DATA: DashboardData = {
  aggregatedBalance: {USD: 100},
  accountCount: 1,
  accountsByType: {checking: 1},
  monthlyFlow: [],
  topCategories: [],
  lastSyncTimestamp: null,
};

describe('dashboardMethods', () => {
  it('setLoading sets status to loading and clears error', () => {
    const state = signalState(initialDashboardState);
    const methods = dashboardMethods(state);
    methods.setError('X');

    methods.setLoading();

    expect(state.status()).toBe('loading');
    expect(state.errorCode()).toBeNull();
  });

  it('setData stores data and returns to idle', () => {
    const state = signalState(initialDashboardState);
    const methods = dashboardMethods(state);
    methods.setError('X');

    methods.setData(SAMPLE_DATA);

    expect(state.data()).toBe(SAMPLE_DATA);
    expect(state.status()).toBe('idle');
    expect(state.errorCode()).toBeNull();
  });

  it('setError flags error status with code', () => {
    const state = signalState(initialDashboardState);
    const methods = dashboardMethods(state);

    methods.setError('BOOM');

    expect(state.status()).toBe('error');
    expect(state.errorCode()).toBe('BOOM');
  });
});
