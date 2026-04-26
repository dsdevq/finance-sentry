import {NgComponentOutlet} from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  Injector,
  type Type,
} from '@angular/core';

import {ConnectStore} from '../../store/connect/connect.store';
import {CONNECT_STRATEGY, ConnectStrategyRegistry} from '../../strategies/connect-strategy.token';
import {BankPickerComponent} from './bank-picker.component';
import {SyncingStateComponent} from './syncing-state.component';
import {TypePickerComponent} from './type-picker.component';

@Component({
  selector: 'fns-connect-modal',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgComponentOutlet, TypePickerComponent, BankPickerComponent, SyncingStateComponent],
  templateUrl: './connect-modal.component.html',
})
export class ConnectModalComponent {
  private readonly parentInjector = inject(Injector);
  private readonly registry = inject(ConnectStrategyRegistry);

  public readonly store = inject(ConnectStore);

  public readonly formComponent = computed<Nullable<Type<unknown>>>(() => {
    const slug = this.providerSlugForCurrentStep();
    return slug ? this.registry.getBySlug(slug).formComponent : null;
  });

  public readonly formInjector = computed<Nullable<Injector>>(() => {
    const slug = this.providerSlugForCurrentStep();
    if (!slug) {
      return null;
    }
    const strategy = this.registry.getBySlug(slug);
    return Injector.create({
      providers: [{provide: CONNECT_STRATEGY, useValue: strategy}],
      parent: this.parentInjector,
    });
  });

  private providerSlugForCurrentStep(): Nullable<'plaid' | 'monobank' | 'binance' | 'ibkr'> {
    switch (this.store.modalStep()) {
      case 'plaid-launcher':
        return 'plaid';
      case 'monobank-form':
        return 'monobank';
      case 'binance-form':
        return 'binance';
      case 'ibkr-form':
        return 'ibkr';
      default:
        return null;
    }
  }
}
