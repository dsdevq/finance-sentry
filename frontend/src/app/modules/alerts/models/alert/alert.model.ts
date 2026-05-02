export type AlertType = 'sync_error' | 'low_balance' | 'unusual_spend' | 'budget' | 'info';
export type AlertSeverity = 'error' | 'warning' | 'info';
export type AlertFilter = 'all' | 'unread' | 'error' | 'warning' | 'info';

export interface Alert {
  id: string;
  type: AlertType;
  severity: AlertSeverity;
  title: string;
  body: string;
  account: Nullable<string>;
  timestamp: number;
  read: boolean;
}
