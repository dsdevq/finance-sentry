import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {
  AlertComponent,
  ButtonComponent,
  DialogActionsComponent,
  FormFieldComponent,
  InputComponent,
} from '@dsdevq-common/ui';

import {HoldingsStore} from '../../../holdings/store/holdings.store';
import {ConnectStore} from '../../store/connect/connect.store';
import {CONNECT_STRATEGY} from '../../strategies/connect-strategy.token';
import {BINANCE_HELP_URL} from './connect-modal.constants';

@Component({
  selector: 'fns-binance-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AlertComponent,
    ButtonComponent,
    DialogActionsComponent,
    FormFieldComponent,
    InputComponent,
    ReactiveFormsModule,
  ],
  templateUrl: './binance-form.component.html',
})
export class BinanceFormComponent {
  private readonly strategy = inject(CONNECT_STRATEGY);
  private readonly holdingsStore = inject(HoldingsStore, {optional: true});

  public readonly store = inject(ConnectStore);

  public readonly form = new FormGroup({
    apiKey: new FormControl<string>('', {nonNullable: true, validators: [Validators.required]}),
    apiSecret: new FormControl<string>('', {nonNullable: true, validators: [Validators.required]}),
  });

  public readonly helpUrl = BINANCE_HELP_URL;

  public readonly isDuplicateError = computed(() => this.store.errorCode() === 'BINANCE_DUPLICATE');

  public submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const {apiKey, apiSecret} = this.form.getRawValue();
    this.store.connect({
      strategy: this.strategy,
      payload: {apiKey: apiKey.trim(), apiSecret: apiSecret.trim()},
    });
  }

  public back(): void {
    this.store.setModalStep('type-picker');
  }

  public disconnectExisting(): void {
    this.holdingsStore?.disconnectBinance();
    this.store.resetError();
    this.form.reset({apiKey: '', apiSecret: ''});
  }
}
