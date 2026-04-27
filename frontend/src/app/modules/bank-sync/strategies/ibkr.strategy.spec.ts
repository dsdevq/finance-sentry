import {TestBed} from '@angular/core/testing';
import {of} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {IBKRService} from '../services/ibkr.service';
import {IbkrConnectStrategy} from './ibkr.strategy';

describe('IbkrConnectStrategy', () => {
  let strategy: IbkrConnectStrategy;
  let ibkr: {connect: ReturnType<typeof vi.fn>};

  beforeEach(() => {
    ibkr = {connect: vi.fn()};
    TestBed.configureTestingModule({
      providers: [{provide: IBKRService, useValue: ibkr}, IbkrConnectStrategy],
    });
    strategy = TestBed.inject(IbkrConnectStrategy);
  });

  it('calls IBKRService.connect with no arguments (single-tenant gateway)', () => {
    ibkr.connect.mockReturnValue(of({holdingsCount: 4, accountId: 'DU123', connectedAt: 'now'}));
    strategy.submit().subscribe();
    expect(ibkr.connect).toHaveBeenCalledWith();
  });

  it('maps the holdingsCount into a broker/POLLING outcome', () => {
    ibkr.connect.mockReturnValue(of({holdingsCount: 4, accountId: 'DU123', connectedAt: 'now'}));
    let outcome: unknown;
    strategy.submit().subscribe(o => (outcome = o));
    expect(outcome).toEqual({successCode: 'POLLING', count: 4, institutionType: 'broker'});
  });

  it('exposes slug "ibkr"', () => {
    expect(strategy.slug).toBe('ibkr');
    expect(strategy.formComponent).toBeDefined();
  });
});
