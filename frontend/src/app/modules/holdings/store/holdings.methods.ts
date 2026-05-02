import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type WealthSummaryResponse} from '../../../shared/models/wealth/wealth.model';
import {type Position} from '../models/position/position.model';
import {type HoldingsState} from './holdings.state';

export function holdingsMethods(store: WritableStateSource<HoldingsState>) {
  return {
    setLoading(): void {
      patchState(store, {status: 'loading', errorCode: null});
    },
    setSummary(summary: WealthSummaryResponse): void {
      patchState(store, {summary, status: 'idle', errorCode: null});
    },
    setError(errorCode: Nullable<string>): void {
      patchState(store, {status: 'error', errorCode});
    },
    setPositionsLoading(): void {
      patchState(store, {positionsStatus: 'loading', positionsErrorCode: null});
    },
    setPositions(positions: Position[]): void {
      patchState(store, {positions, positionsStatus: 'idle', positionsErrorCode: null});
    },
    setPositionsError(errorCode: Nullable<string>): void {
      patchState(store, {positionsStatus: 'error', positionsErrorCode: errorCode});
    },
  };
}
