import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type Position} from '../models/position/position.model';
import {type HoldingsState} from './holdings.state';

export function holdingsMethods(store: WritableStateSource<HoldingsState>) {
  return {
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
