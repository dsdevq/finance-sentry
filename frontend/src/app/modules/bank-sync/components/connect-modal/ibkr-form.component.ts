import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {AlertComponent, ButtonComponent, DialogActionsComponent} from '@dsdevq-common/ui';

import {HoldingsStore} from '../../../holdings/store/holdings.store';
import {ConnectStore} from '../../store/connect/connect.store';
import {CONNECT_STRATEGY} from '../../strategies/connect-strategy.token';

@Component({
  selector: 'fns-ibkr-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AlertComponent, ButtonComponent, DialogActionsComponent],
  templateUrl: './ibkr-form.component.html',
})
export class IbkrFormComponent {
  private readonly strategy = inject(CONNECT_STRATEGY);
  private readonly holdingsStore = inject(HoldingsStore, {optional: true});

  public readonly store = inject(ConnectStore);

  public readonly isDuplicateError = computed(() => this.store.errorCode() === 'IBKR_DUPLICATE');

  public connect(): void {
    this.store.connect({strategy: this.strategy, payload: undefined});
  }

  public back(): void {
    this.store.setModalStep('type-picker');
  }

  public disconnectExisting(): void {
    this.holdingsStore?.disconnectIBKR();
    this.store.resetError();
  }
}
