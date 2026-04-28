import {TestBed} from '@angular/core/testing';
import {of} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {BankSyncService} from '../services/bank-sync.service';
import {MonobankConnectStrategy} from './monobank.strategy';

describe('MonobankConnectStrategy', () => {
  let strategy: MonobankConnectStrategy;
  let bankSync: {connectMonobank: ReturnType<typeof vi.fn>};

  beforeEach(() => {
    bankSync = {connectMonobank: vi.fn()};
    TestBed.configureTestingModule({
      providers: [{provide: BankSyncService, useValue: bankSync}, MonobankConnectStrategy],
    });
    strategy = TestBed.inject(MonobankConnectStrategy);
  });

  it('forwards the token to BankSyncService.connectMonobank', () => {
    bankSync.connectMonobank.mockReturnValue(of({accounts: [{id: '1'}, {id: '2'}]}));
    strategy.submit({token: 'tok'}).subscribe();
    expect(bankSync.connectMonobank).toHaveBeenCalledWith('tok');
  });

  it('maps the response to a bank/POLLING outcome with the account count', () => {
    bankSync.connectMonobank.mockReturnValue(of({accounts: [{id: '1'}, {id: '2'}, {id: '3'}]}));
    let outcome: unknown;
    strategy.submit({token: 'tok'}).subscribe(o => (outcome = o));
    expect(outcome).toEqual({successCode: 'POLLING', count: 3, institutionType: 'bank'});
  });

  it('exposes slug "monobank" and the MonobankFormComponent type', () => {
    expect(strategy.slug).toBe('monobank');
    expect(strategy.formComponent).toBeDefined();
  });
});
