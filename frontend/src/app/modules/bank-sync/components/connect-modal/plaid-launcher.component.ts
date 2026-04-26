import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {ButtonComponent, DialogActionsComponent} from '@dsdevq-common/ui';

import {ConnectStore} from '../../store/connect/connect.store';
import {CONNECT_STRATEGY} from '../../strategies/connect-strategy.token';

@Component({
  selector: 'fns-plaid-launcher',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonComponent, DialogActionsComponent],
  templateUrl: './plaid-launcher.component.html',
})
export class PlaidLauncherComponent {
  private readonly strategy = inject(CONNECT_STRATEGY);

  public readonly store = inject(ConnectStore);

  public launch(): void {
    this.store.connect({strategy: this.strategy, payload: undefined});
  }

  public back(): void {
    this.store.setModalStep('bank-picker');
  }
}
