import {type WealthSummaryResponse} from '../../../shared/models/wealth/wealth.model';
import {type Position} from '../models/position/position.model';

export interface HoldingsState {
  summary: Nullable<WealthSummaryResponse>;
  status: AsyncStatus;
  errorCode: Nullable<string>;
  positions: Position[];
  positionsStatus: AsyncStatus;
  positionsErrorCode: Nullable<string>;
}

export const initialHoldingsState: HoldingsState = {
  summary: null,
  status: 'idle',
  errorCode: null,
  positions: [],
  positionsStatus: 'idle',
  positionsErrorCode: null,
};
