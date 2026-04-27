import {TestBed} from '@angular/core/testing';
import {firstValueFrom, of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {BankSyncService} from '../services/bank-sync.service';
import {PlaidLinkService} from '../services/plaid-link.service';
import {PlaidConnectStrategy} from './plaid.strategy';

interface PlaidPrepareCallbacks {
  readonly token: string;
  readonly onSuccess: (publicToken: string, metadata: {institution: {name: string}}) => void;
}

describe('PlaidConnectStrategy', () => {
  let strategy: PlaidConnectStrategy;
  let bankSync: {
    getLinkToken: ReturnType<typeof vi.fn>;
    exchangePublicToken: ReturnType<typeof vi.fn>;
  };
  let plaid: {
    prepare: ReturnType<typeof vi.fn>;
    open: ReturnType<typeof vi.fn>;
    destroy: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    bankSync = {getLinkToken: vi.fn(), exchangePublicToken: vi.fn()};
    plaid = {prepare: vi.fn(), open: vi.fn(), destroy: vi.fn()};
    TestBed.configureTestingModule({
      providers: [
        {provide: BankSyncService, useValue: bankSync},
        {provide: PlaidLinkService, useValue: plaid},
        PlaidConnectStrategy,
      ],
    });
    strategy = TestBed.inject(PlaidConnectStrategy);
  });

  it('opens Plaid Link after successful link-token + prepare and emits a bank/POLLING outcome on exchange', async () => {
    bankSync.getLinkToken.mockReturnValue(of({linkToken: 'link-1'}));
    let captured: PlaidPrepareCallbacks | undefined;
    plaid.prepare.mockImplementation((cfg: PlaidPrepareCallbacks) => {
      captured = cfg;
      return of(undefined);
    });
    bankSync.exchangePublicToken.mockReturnValue(of({success: true}));

    const outcome$ = firstValueFrom(strategy.submit());
    expect(plaid.open).toHaveBeenCalledOnce();
    captured?.onSuccess('public-1', {institution: {name: 'Wells'}});
    const outcome = await outcome$;

    expect(outcome).toEqual({successCode: 'POLLING', count: 1, institutionType: 'bank'});
    expect(bankSync.exchangePublicToken).toHaveBeenCalledWith('public-1', 'Wells');
  });

  it('maps a getLinkToken failure to PLAID_SCRIPT_LOAD_FAILED', async () => {
    bankSync.getLinkToken.mockReturnValue(throwError(() => new Error('network')));

    await expect(firstValueFrom(strategy.submit())).rejects.toMatchObject({
      errorCode: 'PLAID_SCRIPT_LOAD_FAILED',
    });
  });

  it('maps an exchange failure to PLAID_LINK_FAILED', async () => {
    bankSync.getLinkToken.mockReturnValue(of({linkToken: 'link-1'}));
    let captured: PlaidPrepareCallbacks | undefined;
    plaid.prepare.mockImplementation((cfg: PlaidPrepareCallbacks) => {
      captured = cfg;
      return of(undefined);
    });
    bankSync.exchangePublicToken.mockReturnValue(throwError(() => new Error('exchange-fail')));

    const promise = firstValueFrom(strategy.submit());
    captured?.onSuccess('public-1', {institution: {name: 'Wells'}});
    await expect(promise).rejects.toMatchObject({errorCode: 'PLAID_LINK_FAILED'});
  });

  it('exposes slug "plaid"', () => {
    expect(strategy.slug).toBe('plaid');
    expect(strategy.formComponent).toBeDefined();
  });
});
