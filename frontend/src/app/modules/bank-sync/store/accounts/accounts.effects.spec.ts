import {TestBed} from '@angular/core/testing';
import {of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type WealthSummaryResponse} from '../../../../shared/models/wealth/wealth.model';
import {BankSyncService} from '../../services/bank-sync.service';
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

function buildBankSync() {
  return {
    disconnectMonobank: vi.fn(),
    disconnectAccount: vi.fn(),
  };
}

function configure(
  service: ReturnType<typeof buildService>,
  bankSync: ReturnType<typeof buildBankSync> = buildBankSync()
) {
  TestBed.configureTestingModule({
    providers: [
      {provide: WealthService, useValue: service},
      {provide: BankSyncService, useValue: bankSync},
    ],
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

  it('disconnectMonobank: success triggers reload via load()', () => {
    const store = buildStore();
    const wealth = buildService();
    const bankSync = buildBankSync();
    bankSync.disconnectMonobank.mockReturnValue(of(undefined));
    wealth.getSummary.mockReturnValue(of(FAKE_SUMMARY));
    configure(wealth, bankSync);

    TestBed.runInInjectionContext(() => {
      accountsEffects(store).disconnectMonobank();
    });

    expect(bankSync.disconnectMonobank).toHaveBeenCalledOnce();
    expect(wealth.getSummary).toHaveBeenCalledOnce();
    expect(store.setSummary).toHaveBeenCalledWith(FAKE_SUMMARY);
  });

  it('disconnectMonobank: error path forwards errorCode', () => {
    const store = buildStore();
    const wealth = buildService();
    const bankSync = buildBankSync();
    bankSync.disconnectMonobank.mockReturnValue(
      throwError(() => ({error: {errorCode: 'MONOBANK_DISCONNECT_FAILED'}}))
    );
    configure(wealth, bankSync);

    TestBed.runInInjectionContext(() => {
      accountsEffects(store).disconnectMonobank();
    });

    expect(store.setError).toHaveBeenCalledWith('MONOBANK_DISCONNECT_FAILED');
    expect(wealth.getSummary).not.toHaveBeenCalled();
  });

  it('disconnectAccount: success triggers reload', () => {
    const store = buildStore();
    const wealth = buildService();
    const bankSync = buildBankSync();
    bankSync.disconnectAccount.mockReturnValue(of(undefined));
    wealth.getSummary.mockReturnValue(of(FAKE_SUMMARY));
    configure(wealth, bankSync);

    TestBed.runInInjectionContext(() => {
      accountsEffects(store).disconnectAccount('acct-1');
    });

    expect(bankSync.disconnectAccount).toHaveBeenCalledWith('acct-1');
    expect(wealth.getSummary).toHaveBeenCalledOnce();
  });
});
