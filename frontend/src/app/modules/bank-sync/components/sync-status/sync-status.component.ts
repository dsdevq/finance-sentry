import {CommonModule} from '@angular/common';
import {ChangeDetectionStrategy, Component, effect, inject, input} from '@angular/core';

import {RelativeTimePipe} from '../../../../shared/pipes/relative-time.pipe';
import {SyncStatusStore} from '../../store/sync-status/sync-status.store';

@Component({
  selector: 'fns-sync-status',
  imports: [CommonModule, RelativeTimePipe],
  templateUrl: './sync-status.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SyncStatusStore],
})
export class SyncStatusComponent {
  public readonly accountId = input.required<string>();
  public readonly store = inject(SyncStatusStore);

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
