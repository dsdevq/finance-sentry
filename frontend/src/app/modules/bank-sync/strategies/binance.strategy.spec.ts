import {TestBed} from '@angular/core/testing';
import {of} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {BinanceService} from '../services/binance.service';
import {BinanceConnectStrategy} from './binance.strategy';

describe('BinanceConnectStrategy', () => {
  let strategy: BinanceConnectStrategy;
  let binance: {connect: ReturnType<typeof vi.fn>};

  beforeEach(() => {
    binance = {connect: vi.fn()};
    TestBed.configureTestingModule({
      providers: [{provide: BinanceService, useValue: binance}, BinanceConnectStrategy],
    });
    strategy = TestBed.inject(BinanceConnectStrategy);
  });

  it('forwards the credential payload to BinanceService.connect', () => {
    binance.connect.mockReturnValue(of({connected: true}));
    const payload = {apiKey: 'k', apiSecret: 's'};
    strategy.submit(payload).subscribe();
    expect(binance.connect).toHaveBeenCalledWith(payload);
  });

  it('maps the response to a crypto/POLLING outcome', () => {
    binance.connect.mockReturnValue(of({connected: true}));
    let outcome: unknown;
    strategy.submit({apiKey: 'k', apiSecret: 's'}).subscribe(o => (outcome = o));
    expect(outcome).toEqual({successCode: 'POLLING', count: 1, institutionType: 'crypto'});
  });

  it('exposes slug "binance"', () => {
    expect(strategy.slug).toBe('binance');
    expect(strategy.formComponent).toBeDefined();
  });
});
