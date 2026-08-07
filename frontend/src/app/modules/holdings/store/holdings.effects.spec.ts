import {TestBed} from '@angular/core/testing';
import {of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type Position} from '../models/position/position.model';
import {PositionsService} from '../services/positions.service';
import {holdingsEffects} from './holdings.effects';

const FAKE_POSITIONS: Position[] = [];

function buildStore() {
  return {
    setPositionsLoading: vi.fn(),
    setPositions: vi.fn(),
    setPositionsError: vi.fn(),
  };
}

function buildPositions() {
  return {getPositions: vi.fn().mockReturnValue(of(FAKE_POSITIONS))};
}

function configure(positions: ReturnType<typeof buildPositions>): void {
  TestBed.configureTestingModule({
    providers: [{provide: PositionsService, useValue: positions}],
  });
}

describe('holdingsEffects', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('loadPositions: success path patches positions', () => {
    const store = buildStore();
    const positions = buildPositions();
    configure(positions);

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
    configure(positions);

    TestBed.runInInjectionContext(() => {
      holdingsEffects(store).loadPositions();
    });

    expect(store.setPositionsError).toHaveBeenCalledWith('POSITIONS_FETCH_FAILED');
  });
});
