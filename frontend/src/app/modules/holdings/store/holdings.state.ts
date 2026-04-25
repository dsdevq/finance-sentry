import {type WealthSummaryResponse} from '../../bank-sync/models/wealth.model';

export type HoldingsStatus = 'idle' | 'loading' | 'error';

export interface HoldingsState {
  summary: WealthSummaryResponse | null;
  status: HoldingsStatus;
  errorCode: string | null;
}

export const initialHoldingsState: HoldingsState = {
  summary: null,
  status: 'idle',
  errorCode: null,
};
