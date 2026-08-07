import {type Position} from '../models/position/position.model';

export interface HoldingsState {
  positions: Position[];
  positionsStatus: AsyncStatus;
  positionsErrorCode: Nullable<string>;
}

export const initialHoldingsState: HoldingsState = {
  positions: [],
  positionsStatus: 'idle',
  positionsErrorCode: null,
};
