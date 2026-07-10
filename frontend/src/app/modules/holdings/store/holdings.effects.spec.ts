import {TestBed} from '@angular/core/testing';
import {of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type WealthSummaryResponse} from '../../../shared/models/wealth/wealth.model';
import {type Position} from '../models/position/position.model';
import {HoldingsService} from '../services/holdings.service';
import {PositionsService} from '../services/positions.service';
import {holdingsEffects} from './holdings.effects';

const FAKE_SUMMARY: WealthSummaryResponse = {
  totalNetWorth: 0,
  baseCurrency: 'USD',
  categories: [],
  appliedFilters: {category: null, provider: null},
};

const FAKE_POSITIONS: Position[] = [];

function buildStore() {
  return {
    setLoading: vi.fn(),
    setSummary: vi.fn(),
    setError: vi.fn(),
    setPositionsLoading: vi.fn(),
    setPositions: vi.fn(),
    setPositionsError: vi.fn(),
  };
}

function buildHoldings() {
  return {getSummary: vi.fn().mockReturnValue(of(FAKE_SUMMARY))};
}

function buildPositions() {
  return {getPositions: vi.fn().mockReturnValue(of(FAKE_POSITIONS))};
}

function configure(
  holdings: ReturnType<typeof buildHoldings>,
  positions: ReturnType<typeof buildPositions>
): void {
  TestBed.configureTestingModule({
    providers: [
      {provide: HoldingsService, useValue: holdings},
      {provide: PositionsService, useValue: positions},
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
    configure(holdings, buildPositions());

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
    configure(holdings, buildPositions());

    TestBed.runInInjectionContext(() => {
      holdingsEffects(store).load();
    });

    expect(store.setError).toHaveBeenCalledWith('HOLDINGS_FETCH_FAILED');
  });

  it('loadPositions: success path patches positions', () => {
    const store = buildStore();
    const positions = buildPositions();
    configure(buildHoldings(), positions);

    TestBed.runInInjectionContext(() => {
      holdingsEffects(store).loadPositions();
    });

    expect(store.setPositionsLoading).toHaveBeenCalledOnce();
    expect(store.setPositions).toHaveBeenCalledWith(FAKE_POSITIONS);
  });

  it('loadPositions: error path forwards errorCode', () => {
    const store = buildStore();
    const positions = buildPositions();
    positions.getPositions.mockReturnValue(
      throwError(() => ({error: {errorCode: 'POSITIONS_FETCH_FAILED'}}))
    );
    configure(buildHoldings(), positions);

    TestBed.runInInjectionContext(() => {
      holdingsEffects(store).loadPositions();
    });

    expect(store.setPositionsError).toHaveBeenCalledWith('POSITIONS_FETCH_FAILED');
  });
});
