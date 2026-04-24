import {CommonModule} from '@angular/common';
import {ChangeDetectionStrategy, Component, effect, inject, input} from '@angular/core';

import {getRelativeTime} from '../../../../shared/utils/relative-time';
import {SyncStatusStore} from '../../store/sync-status/sync-status.store';

@Component({
  selector: 'fns-sync-status',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sync-status.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SyncStatusStore],
})
export class SyncStatusComponent {
  public readonly accountId = input.required<string>();
  public readonly store = inject(SyncStatusStore);
  public readonly getRelativeTime = getRelativeTime;

  constructor() {
    effect(() => {
      const id = this.accountId();
      if (!id) {
        return;
      }
      this.store.setAccountId(id);
      this.store.loadStatus();
    });
  }

  public triggerSync(): void {
    this.store.triggerSync();
  }
}
