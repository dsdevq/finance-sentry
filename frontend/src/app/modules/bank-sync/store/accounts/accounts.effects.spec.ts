import {TestBed} from '@angular/core/testing';
import {of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type WealthSummaryResponse} from '../../models/wealth.model';
import {WealthService} from '../../services/wealth.service';
import {accountsEffects} from './accounts.effects';

const FAKE_SUMMARY: WealthSummaryResponse = {
  totalNetWorth: 1000,
  baseCurrency: 'USD',
  categories: [],
  appliedFilters: {category: null, provider: null},
};

function buildStore() {
  return {
    setLoading: vi.fn(),
    setSummary: vi.fn(),
    setError: vi.fn(),
  };
}

function buildService() {
  return {
    getSummary: vi.fn(),
  };
}

function configure(service: ReturnType<typeof buildService>) {
  TestBed.configureTestingModule({
    providers: [{provide: WealthService, useValue: service}],
  });
}

describe('accountsEffects', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('load: success path', () => {
    const store = buildStore();
    const service = buildService();
    service.getSummary.mockReturnValue(of(FAKE_SUMMARY));
    configure(service);

    TestBed.runInInjectionContext(() => {
      accountsEffects(store).load();
    });

    expect(store.setLoading).toHaveBeenCalled();
    expect(store.setSummary).toHaveBeenCalledWith(FAKE_SUMMARY);
    expect(store.setError).not.toHaveBeenCalled();
  });

  it('load: error path extracts errorCode', () => {
    const store = buildStore();
    const service = buildService();
    service.getSummary.mockReturnValue(throwError(() => ({error: {errorCode: 'API_DOWN'}})));
    configure(service);

    TestBed.runInInjectionContext(() => {
      accountsEffects(store).load();
    });

    expect(store.setError).toHaveBeenCalledWith('API_DOWN');
    expect(store.setSummary).not.toHaveBeenCalled();
  });
});
