import {Pipe, type PipeTransform} from '@angular/core';

import {type SyncStatus} from '../models/wealth/wealth.model';

type BadgeVariant = 'success' | 'warning' | 'error';

const VARIANT_MAP: Record<SyncStatus, BadgeVariant> = {
  synced: 'success',
  pending: 'warning',
  syncing: 'warning',
  stale: 'warning',
  failed: 'error',
  // eslint-disable-next-line @typescript-eslint/naming-convention
  reauth_required: 'error',
};

@Pipe({name: 'syncStatusVariant'})
export class SyncStatusVariantPipe implements PipeTransform {
  public transform(syncStatus: SyncStatus): BadgeVariant {
    return VARIANT_MAP[syncStatus];
  }
}
