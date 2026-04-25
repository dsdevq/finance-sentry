/* eslint-disable @typescript-eslint/naming-convention */
import {TestBed} from '@angular/core/testing';
import {Router} from '@angular/router';
import {of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {
  type AccountsResponse,
  type BankAccount,
} from '../../models/bank-account/bank-account.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {PlaidLinkService} from '../../services/plaid-link.service';
import {connectEffects} from './connect.effects';

function buildStore() {
  return {
    setInitializing: vi.fn(),
    setReady: vi.fn(),
    setSyncing: vi.fn(),
    setPolling: vi.fn(),
    setSuccess: vi.fn(),
    setError: vi.fn(),
  };
}

function buildService() {
  return {
    getLinkToken: vi.fn(),
    exchangePublicToken: vi.fn(),
    connectMonobank: vi.fn(),
    getAccounts: vi.fn(),
  };
}

function buildPlaid() {
  return {
    prepare: vi.fn().mockReturnValue(of(undefined)),
    open: vi.fn(),
    destroy: vi.fn(),
  };
}

function configure(
  service: ReturnType<typeof buildService>,
  plaid: ReturnType<typeof buildPlaid>,
  router = {navigate: vi.fn()} as unknown as Router
) {
  TestBed.configureTestingModule({
    providers: [
      {provide: BankSyncService, useValue: service},
      {provide: PlaidLinkService, useValue: plaid},
      {provide: Router, useValue: router},
    ],
  });
}

const ACTIVE_RESPONSE: AccountsResponse = {
  accounts: [{accountId: 'a1', syncStatus: 'active'} as unknown as BankAccount],
  totalCount: 1,
  currency_totals: {},
};

describe('connectEffects', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  describe('initPlaid', () => {
    it('sets initializing, fetches token, prepares plaid, then sets ready', () => {
      const store = buildStore();
      const service = buildService();
      service.getLinkToken.mockReturnValue(of({linkToken: 't'}));
      const plaid = buildPlaid();
      configure(service, plaid);

      TestBed.runInInjectionContext(() => connectEffects(store).initPlaid());

      expect(store.setInitializing).toHaveBeenCalled();
      expect(service.getLinkToken).toHaveBeenCalled();
      expect(plaid.prepare).toHaveBeenCalled();
      expect(store.setReady).toHaveBeenCalled();
      expect(store.setError).not.toHaveBeenCalled();
    });

    it('sets error on getLinkToken failure', () => {
      const store = buildStore();
      const service = buildService();
      service.getLinkToken.mockReturnValue(throwError(() => ({error: {errorCode: 'PLAID_DOWN'}})));
      const plaid = buildPlaid();
      configure(service, plaid);

      TestBed.runInInjectionContext(() => connectEffects(store).initPlaid());

      expect(store.setError).toHaveBeenCalledWith('PLAID_DOWN');
      expect(plaid.prepare).not.toHaveBeenCalled();
    });
  });

  describe('openPlaid', () => {
    it('delegates to PlaidLinkService.open', () => {
      const plaid = buildPlaid();
      configure(buildService(), plaid);

      TestBed.runInInjectionContext(() => connectEffects(buildStore()).openPlaid());

      expect(plaid.open).toHaveBeenCalled();
    });
  });

  describe('connectMonobank', () => {
    it('success path sets syncing, calls service, and kicks off polling', () => {
      const store = buildStore();
      const service = buildService();
      service.connectMonobank.mockReturnValue(of({accounts: []}));
      service.getAccounts.mockReturnValue(of(ACTIVE_RESPONSE));
      configure(service, buildPlaid());

      TestBed.runInInjectionContext(() => connectEffects(store).connectMonobank('token'));

      expect(store.setSyncing).toHaveBeenCalledWith('Connecting your Monobank account...');
      expect(service.connectMonobank).toHaveBeenCalledWith('token');
      expect(store.setPolling).toHaveBeenCalled();
    });

    it('error path maps errorCode', () => {
      const store = buildStore();
      const service = buildService();
      service.connectMonobank.mockReturnValue(
        throwError(() => ({error: {errorCode: 'MONOBANK_TOKEN_INVALID'}}))
      );
      configure(service, buildPlaid());

      TestBed.runInInjectionContext(() => connectEffects(store).connectMonobank('bad'));

      expect(store.setError).toHaveBeenCalledWith('MONOBANK_TOKEN_INVALID');
    });
  });

  describe('exchangePlaidToken', () => {
    it('success triggers polling', () => {
      const store = buildStore();
      const service = buildService();
      service.exchangePublicToken.mockReturnValue(of({} as unknown));
      service.getAccounts.mockReturnValue(of(ACTIVE_RESPONSE));
      configure(service, buildPlaid());

      TestBed.runInInjectionContext(() =>
        connectEffects(store).exchangePlaidToken({
          publicToken: 'pt',
          metadata: {
            institution: {name: 'Chase', institution_id: 'ins_1'},
            accounts: [],
            link_session_id: 's',
          },
        })
      );

      expect(service.exchangePublicToken).toHaveBeenCalledWith('pt', 'Chase');
      expect(store.setPolling).toHaveBeenCalled();
    });

    it('falls back to PLAID_LINK_FAILED on unstructured errors', () => {
      const store = buildStore();
      const service = buildService();
      service.exchangePublicToken.mockReturnValue(throwError(() => new Error('net')));
      configure(service, buildPlaid());

      TestBed.runInInjectionContext(() =>
        connectEffects(store).exchangePlaidToken({
          publicToken: 'pt',
          metadata: {institution: null, accounts: [], link_session_id: 's'},
        })
      );

      expect(store.setError).toHaveBeenCalledWith('PLAID_LINK_FAILED');
    });
  });
});
