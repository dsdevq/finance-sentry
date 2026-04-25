import {CommonModule} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {FormControl, ReactiveFormsModule, Validators} from '@angular/forms';
import {
  AlertComponent,
  ButtonComponent,
  FormFieldComponent,
  InputComponent,
} from '@dsdevq-common/ui';

import {type Provider} from '../../models/bank-account/bank-account.model';
import {ConnectStore} from '../../store/connect/connect.store';
import {MONOBANK_TOKEN_MAX_LENGTH} from './connect-account.constants';

@Component({
  selector: 'fns-connect-account',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    AlertComponent,
    ButtonComponent,
    FormFieldComponent,
    InputComponent,
  ],
  templateUrl: './connect-account.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConnectStore],
})
export class ConnectAccountComponent {
  public readonly store = inject(ConnectStore);

  public readonly monobankToken = new FormControl<string>('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(MONOBANK_TOKEN_MAX_LENGTH)],
  });

  public selectProvider(provider: Provider): void {
    this.store.selectProvider(provider);
  }

  public openPlaidLink(): void {
    this.store.openPlaid();
  }

  public connectMonobank(): void {
    if (this.monobankToken.invalid) {
      this.monobankToken.markAsTouched();
      return;
    }
    this.store.connectMonobank(this.monobankToken.value);
  }
}
