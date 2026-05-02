import {type Alert, type AlertFilter} from '../../models/alert/alert.model';

export interface AlertsState {
  alerts: Alert[];
  filter: AlertFilter;
  status: AsyncStatus;
}

export const initialAlertsState: AlertsState = {
  alerts: [],
  filter: 'all',
  status: 'idle',
};
