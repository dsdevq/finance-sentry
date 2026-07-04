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
} satisfies Record<AlertType, AlertTypeMeta>;
