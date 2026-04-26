import {ChangeDetectionStrategy, Component, inject} from '@angular/core';

import {ConnectStore} from '../../store/connect/connect.store';

@Component({
  selector: 'fns-syncing-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './syncing-state.component.html',
})
export class SyncingStateComponent {
  public readonly store = inject(ConnectStore);
}
