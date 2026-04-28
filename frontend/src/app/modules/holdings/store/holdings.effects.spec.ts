import {TestBed} from '@angular/core/testing';
import {of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type WealthSummaryResponse} from '../../../shared/models/wealth/wealth.model';
import {BinanceService} from '../../bank-sync/services/binance.service';
import {IBKRService} from '../../bank-sync/services/ibkr.service';
import {HoldingsService} from '../services/holdings.service';
import {holdingsEffects} from './holdings.effects';

const FAKE_SUMMARY: WealthSummaryResponse = {
  totalNetWorth: 0,
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

function buildHoldings() {
  return {getSummary: vi.fn().mockReturnValue(of(FAKE_SUMMARY))};
}

function buildBinance() {
  return {disconnect: vi.fn()};
}

function buildIBKR() {
  return {disconnect: vi.fn()};
}

function configure(
  holdings: ReturnType<typeof buildHoldings>,
  binance: ReturnType<typeof buildBinance>,
  ibkr: ReturnType<typeof buildIBKR>
): void {
  TestBed.configureTestingModule({
    providers: [
      {provide: HoldingsService, useValue: holdings},
      {provide: BinanceService, useValue: binance},
      {provide: IBKRService, useValue: ibkr},
    ],
  });
}

describe('holdingsEffects', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('load: success path patches summary', () => {
    const store = buildStore();
    const holdings = buildHoldings();
    configure(holdings, buildBinance(), buildIBKR());

    TestBed.runInInjectionContext(() => {
      holdingsEffects(store).load();
    });

    expect(store.setLoading).toHaveBeenCalledOnce();
    expect(store.setSummary).toHaveBeenCalledWith(FAKE_SUMMARY);
  });

  it('load: error path forwards errorCode', () => {
    const store = buildStore();
    const holdings = buildHoldings();
    holdings.getSummary.mockReturnValue(
      throwError(() => ({error: {errorCode: 'HOLDINGS_FETCH_FAILED'}}))
    );
    configure(holdings, buildBinance(), buildIBKR());

    TestBed.runInInjectionContext(() => {
      holdingsEffects(store).load();
    });

    expect(store.setError).toHaveBeenCalledWith('HOLDINGS_FETCH_FAILED');
  });

  it('disconnectBinance: success triggers reload via load()', () => {
    const store = buildStore();
    const holdings = buildHoldings();
    const binance = buildBinance();
    binance.disconnect.mockReturnValue(of(undefined));
    configure(holdings, binance, buildIBKR());

    TestBed.runInInjectionContext(() => {
      holdingsEffects(store).disconnectBinance();
    });

    expect(binance.disconnect).toHaveBeenCalledOnce();
    expect(holdings.getSummary).toHaveBeenCalledOnce();
    expect(store.setSummary).toHaveBeenCalledWith(FAKE_SUMMARY);
  });

  it('disconnectBinance: error path forwards errorCode', () => {
    const store = buildStore();
    const holdings = buildHoldings();
    const binance = buildBinance();
    binance.disconnect.mockReturnValue(
      throwError(() => ({error: {errorCode: 'BINANCE_DISCONNECT_FAILED'}}))
    );
    configure(holdings, binance, buildIBKR());

    TestBed.runInInjectionContext(() => {
      holdingsEffects(store).disconnectBinance();
    });

    expect(store.setError).toHaveBeenCalledWith('BINANCE_DISCONNECT_FAILED');
    expect(holdings.getSummary).not.toHaveBeenCalled();
  });

  it('disconnectIBKR: success triggers reload', () => {
    const store = buildStore();
    const holdings = buildHoldings();
    const ibkr = buildIBKR();
    ibkr.disconnect.mockReturnValue(of(undefined));
    configure(holdings, buildBinance(), ibkr);

    TestBed.runInInjectionContext(() => {
      holdingsEffects(store).disconnectIBKR();
    });

    expect(ibkr.disconnect).toHaveBeenCalledOnce();
    expect(holdings.getSummary).toHaveBeenCalledOnce();
  });

  it('disconnectIBKR: error path forwards errorCode', () => {
    const store = buildStore();
    const holdings = buildHoldings();
    const ibkr = buildIBKR();
    ibkr.disconnect.mockReturnValue(
      throwError(() => ({error: {errorCode: 'IBKR_DISCONNECT_FAILED'}}))
    );
    configure(holdings, buildBinance(), ibkr);

    TestBed.runInInjectionContext(() => {
      holdingsEffects(store).disconnectIBKR();
    });

    expect(store.setError).toHaveBeenCalledWith('IBKR_DISCONNECT_FAILED');
  });
});
