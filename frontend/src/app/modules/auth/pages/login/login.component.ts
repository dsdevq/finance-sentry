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

@Component({
  selector: 'fns-login',
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
  templateUrl: './login.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private readonly authStore = inject(AuthStore);

  public readonly form = inject(FormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  public readonly googleClientId = environment.googleClientId;
  public readonly AppRoute = AppRoute;
  public readonly loading = this.authStore.isLoading;
  public readonly errorMessage = this.authStore.errorMessage;
  public readonly flashMessage = this.authStore.flashMessage;

  public onSubmit(): void {
    if (this.form.invalid) {
      return;
    }
    const {email, password} = this.form.value;
    this.authStore.login({email: email ?? '', password: password ?? ''});
  }

  public onGoogleCredential(credential: string): void {
    this.authStore.verifyGoogleCredential(credential);
  }
}
