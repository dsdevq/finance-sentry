import {type WealthSummaryResponse} from '../../../shared/models/wealth/wealth.model';

export interface HoldingsState {
  summary: Nullable<WealthSummaryResponse>;
  status: AsyncStatus;
  errorCode: Nullable<string>;
}

export const initialHoldingsState: HoldingsState = {
  summary: null,
  status: 'idle',
  errorCode: null,
};
