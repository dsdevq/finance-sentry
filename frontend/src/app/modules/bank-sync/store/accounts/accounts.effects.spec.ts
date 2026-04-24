import {TestBed} from '@angular/core/testing';
import {of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type AccountsResponse, type BankAccount} from '../../models/bank-account.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {accountsEffects} from './accounts.effects';

const ACCOUNTS: BankAccount[] = [{accountId: 'a1'} as unknown as BankAccount];
const RESPONSE: AccountsResponse = {
  accounts: ACCOUNTS,
  totalCount: 1,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  currency_totals: {},
};

function buildStore() {
  return {
    setLoading: vi.fn(),
    setAccounts: vi.fn(),
    setError: vi.fn(),
    removeAccount: vi.fn(),
  };
}

function buildService() {
  return {
    getAccounts: vi.fn(),
    disconnectAccount: vi.fn(),
  };
}

function configure(service: ReturnType<typeof buildService>) {
  TestBed.configureTestingModule({
    providers: [{provide: BankSyncService, useValue: service}],
  });
}

describe('accountsEffects', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('load: success path', () => {
    const store = buildStore();
    const service = buildService();
    service.getAccounts.mockReturnValue(of(RESPONSE));
    configure(service);

    TestBed.runInInjectionContext(() => {
      accountsEffects(store).load();
    });

    expect(store.setLoading).toHaveBeenCalled();
    expect(store.setAccounts).toHaveBeenCalledWith(ACCOUNTS);
    expect(store.setError).not.toHaveBeenCalled();
  });

  it('load: error path extracts errorCode', () => {
    const store = buildStore();
    const service = buildService();
    service.getAccounts.mockReturnValue(throwError(() => ({error: {errorCode: 'API_DOWN'}})));
    configure(service);

    TestBed.runInInjectionContext(() => {
      accountsEffects(store).load();
    });

    expect(store.setError).toHaveBeenCalledWith('API_DOWN');
    expect(store.setAccounts).not.toHaveBeenCalled();
  });

  it('disconnect: success removes account', () => {
    const store = buildStore();
    const service = buildService();
    service.disconnectAccount.mockReturnValue(of(undefined));
    configure(service);

    TestBed.runInInjectionContext(() => {
      accountsEffects(store).disconnect('a1');
    });

    expect(service.disconnectAccount).toHaveBeenCalledWith('a1');
    expect(store.removeAccount).toHaveBeenCalledWith('a1');
  });

  it('disconnect: error sets error code and does not remove', () => {
    const store = buildStore();
    const service = buildService();
    service.disconnectAccount.mockReturnValue(throwError(() => ({error: {errorCode: 'DENIED'}})));
    configure(service);

    TestBed.runInInjectionContext(() => {
      accountsEffects(store).disconnect('a1');
    });

    expect(store.setError).toHaveBeenCalledWith('DENIED');
    expect(store.removeAccount).not.toHaveBeenCalled();
  });
});
