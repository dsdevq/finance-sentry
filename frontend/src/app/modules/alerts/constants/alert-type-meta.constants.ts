import {type LucideIconName} from '@dsdevq-common/ui';

import {type AlertType} from '../models/alert/alert.model';

export interface AlertTypeMeta {
  icon: LucideIconName;
  label: string;
  description: string;
}

export const ALERT_TYPE_META_REGISTRY = {
  ['LowBalance']: {
    icon: 'TriangleAlert',
    label: 'low balance',
    description: 'Balance dropped below your alert threshold.',
  },
  ['SyncFailure']: {
    icon: 'CircleAlert',
    label: 'sync error',
    description: 'The provider failed to refresh this account.',
  },
  ['UnusualSpend']: {
    icon: 'Zap',
    label: 'unusual spend',
    description: 'This category is spending faster than usual.',
  },
  ['PolicyViolation']: {
    icon: 'ShieldAlert',
    label: 'policy violation',
    description: 'A holding breached one of your risk-policy rules.',
  },
  ['Opportunity']: {
    icon: 'Lightbulb',
    label: 'opportunity',
    description: 'A potential opportunity surfaced from the research scan.',
  },
} satisfies Record<AlertType, AlertTypeMeta>;

/**
 * Fallback for alert types the backend may add before the frontend registry catches up —
 * keeps the alerts page from crashing on an unmapped type.
 */
export const DEFAULT_ALERT_TYPE_META: AlertTypeMeta = {
  icon: 'Bell',
  label: 'alert',
  description: 'You have a new alert.',
};
