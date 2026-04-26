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

@Component({
  selector: 'fns-ibkr-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AlertComponent,
    ButtonComponent,
    DialogActionsComponent,
    FormFieldComponent,
    InputComponent,
    ReactiveFormsModule,
  ],
  templateUrl: './ibkr-form.component.html',
})
export class IbkrFormComponent {
  private readonly strategy = inject(CONNECT_STRATEGY);
  private readonly holdingsStore = inject(HoldingsStore, {optional: true});

  public readonly store = inject(ConnectStore);

  public readonly form = new FormGroup({
    username: new FormControl<string>('', {nonNullable: true, validators: [Validators.required]}),
    password: new FormControl<string>('', {nonNullable: true, validators: [Validators.required]}),
  });

  public readonly isDuplicateError = computed(() => this.store.errorCode() === 'IBKR_DUPLICATE');

  public submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const {username, password} = this.form.getRawValue();
    this.store.connect({
      strategy: this.strategy,
      payload: {username: username.trim(), password},
    });
  }

  public back(): void {
    this.store.setModalStep('type-picker');
  }

  public disconnectExisting(): void {
    this.holdingsStore?.disconnectIBKR();
    this.store.resetError();
    this.form.reset({username: '', password: ''});
  }
}
