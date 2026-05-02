import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {toSignal} from '@angular/core/rxjs-interop';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {
  AlertComponent,
  ButtonComponent,
  CmnPasswordStrengthComponent,
  FormFieldComponent,
  GoogleSignInButtonComponent,
  InputComponent,
} from '@dsdevq-common/ui';

import {environment} from '../../../../../environments/environment';
import {AppRoute} from '../../../../shared/enums/app-route/app-route.enum';
import {AuthStore} from '../../store/auth.store';

const MIN_PASSWORD_LENGTH = 8;

@Component({
  selector: 'fns-register',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    ButtonComponent,
    CmnPasswordStrengthComponent,
    FormFieldComponent,
    GoogleSignInButtonComponent,
    InputComponent,
  ],
  templateUrl: './register.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  private readonly authStore = inject(AuthStore);
  private readonly formGroup = inject(FormBuilder).group({
    firstName: [''],
    lastName: [''],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(MIN_PASSWORD_LENGTH)]],
  });
  private readonly passwordValue = toSignal(this.formGroup.controls.password.valueChanges, {
    initialValue: '',
  });

  public readonly form = this.formGroup;
  public readonly passwordStrength = computed(() => {
    const pw = this.passwordValue() ?? '';
    if (pw.length === 0) {
      return 0;
    }
    let score = 0;
    if (pw.length >= MIN_PASSWORD_LENGTH) {
      score++;
    }
    if (/[A-Z]/.test(pw)) {
      score++;
    }
    if (/[0-9]/.test(pw)) {
      score++;
    }
    if (/[^A-Za-z0-9]/.test(pw)) {
      score++;
    }
    return score;
  });

  public readonly googleClientId = environment.googleClientId;
  public readonly AppRoute = AppRoute;
  public readonly loading = this.authStore.isLoading;
  public readonly errorMessage = this.authStore.errorMessage;

  public onSubmit(): void {
    if (this.form.invalid) {
      return;
    }
    const {email, password} = this.form.value;
    this.authStore.register({email: email ?? '', password: password ?? ''});
  }

  public onGoogleCredential(credential: string): void {
    this.authStore.verifyGoogleCredential(credential);
  }
}
