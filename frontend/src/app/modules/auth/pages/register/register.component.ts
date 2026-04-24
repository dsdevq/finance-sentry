import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {
  AlertComponent,
  ButtonComponent,
  FormFieldComponent,
  GoogleSignInButtonComponent,
  InputComponent,
} from '@dsdevq-common/ui';

import {environment} from '../../../../../environments/environment';
import {AppRoute} from '../../../../shared/enums/app-route.enum';
import {AuthStore} from '../../store/auth.store';
import {passwordsMatch} from '../../validators/password-match.validator';

const MIN_PASSWORD_LENGTH = 8;

@Component({
  selector: 'fns-register',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    ButtonComponent,
    FormFieldComponent,
    GoogleSignInButtonComponent,
    InputComponent,
  ],
  templateUrl: './register.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  private readonly authStore = inject(AuthStore);

  public readonly form = inject(FormBuilder).group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(MIN_PASSWORD_LENGTH)]],
      confirmPassword: ['', Validators.required],
    },
    {validators: passwordsMatch}
  );
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
