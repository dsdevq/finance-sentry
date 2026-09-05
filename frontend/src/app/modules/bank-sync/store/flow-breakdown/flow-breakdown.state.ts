import {type FlowBreakdown} from '../../models/flow-breakdown/flow-breakdown.model';

export interface FlowBreakdownState {
  breakdown: Nullable<FlowBreakdown>;
  month: string;
  accountFilter: Nullable<string>;
  status: AsyncStatus;
  errorCode: Nullable<string>;
}

export const initialFlowBreakdownState: FlowBreakdownState = {
  breakdown: null,
  month: '',
  accountFilter: null,
  status: 'idle',
  errorCode: null,
};
