import {TestBed} from '@angular/core/testing';
import {of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type DashboardData} from '../../models/dashboard.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {dashboardEffects} from './dashboard.effects';

const SAMPLE_DATA: DashboardData = {
  aggregatedBalance: {USD: 100},
  accountCount: 1,
  accountsByType: {checking: 1},
  monthlyFlow: [],
  topCategories: [],
  lastSyncTimestamp: null,
};

function buildStore() {
  return {
    setLoading: vi.fn(),
    setData: vi.fn(),
    setError: vi.fn(),
  };
}

function buildService() {
  return {
    getDashboardData: vi.fn(),
  };
}

function configure(service: ReturnType<typeof buildService>) {
  TestBed.configureTestingModule({
    providers: [{provide: BankSyncService, useValue: service}],
  });
}

describe('dashboardEffects', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('load: sets loading, calls service, stores data on success', () => {
    const store = buildStore();
    const service = buildService();
    service.getDashboardData.mockReturnValue(of(SAMPLE_DATA));
    configure(service);

    TestBed.runInInjectionContext(() => {
      const effects = dashboardEffects(store);
      effects.load();
    });

    expect(store.setLoading).toHaveBeenCalled();
    expect(service.getDashboardData).toHaveBeenCalled();
    expect(store.setData).toHaveBeenCalledWith(SAMPLE_DATA);
    expect(store.setError).not.toHaveBeenCalled();
  });

  it('load: sets error with extracted code on failure', () => {
    const store = buildStore();
    const service = buildService();
    service.getDashboardData.mockReturnValue(throwError(() => ({error: {errorCode: 'API_DOWN'}})));
    configure(service);

    TestBed.runInInjectionContext(() => {
      dashboardEffects(store).load();
    });

    expect(store.setError).toHaveBeenCalledWith('API_DOWN');
    expect(store.setData).not.toHaveBeenCalled();
  });

  it('load: sets null code when error is unstructured', () => {
    const store = buildStore();
    const service = buildService();
    service.getDashboardData.mockReturnValue(throwError(() => new Error('net')));
    configure(service);

    TestBed.runInInjectionContext(() => {
      dashboardEffects(store).load();
    });

    expect(store.setError).toHaveBeenCalledWith(null);
  });
});
