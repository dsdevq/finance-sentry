import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import {Router, RouterLink} from '@angular/router';
import {
  AlertComponent,
  ButtonComponent,
  FormFieldComponent,
  GoogleSignInButtonComponent,
  InputComponent,
} from '@dsdevq-common/ui';

import {environment} from '../../../../../environments/environment';
import {AppRoute} from '../../../../shared/enums/app-route.enum';
import {AuthService} from '../../services/auth.service';

const MIN_PASSWORD_LENGTH = 8;
const DUPLICATE_EMAIL_CODE = 'DUPLICATE_EMAIL';

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value as string | undefined;
  const confirm = group.get('confirmPassword')?.value as string | undefined;
  const ctrl = group.get('confirmPassword');
  if (!ctrl) {
    return null;
  }
  if (password !== confirm) {
    const existing = ctrl.errors ?? {};
    ctrl.setErrors({...existing, passwordsMismatch: true});
  } else if (ctrl.errors?.['passwordsMismatch']) {
    const rest = Object.fromEntries(
      Object.entries(ctrl.errors).filter(([key]) => key !== 'passwordsMismatch')
    );
    ctrl.setErrors(Object.keys(rest).length ? rest : null);
  }
  return null;
}

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
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

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
  public errorMessage = '';
  public loading = false;

  public onSubmit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const {email, password} = this.form.value;
    this.authService.register({email: email ?? '', password: password ?? ''}).subscribe({
      next: () => {
        void this.router.navigate([AppRoute.Accounts]);
      },
      error: (err: unknown) => {
        const code = (err as {error?: {errorCode?: string}} | null)?.error?.errorCode;
        if (code === DUPLICATE_EMAIL_CODE) {
          this.errorMessage = 'Email is already registered.';
          this.loading = false;
        }
      },
    });
  }

  public onGoogleCredential(credential: string): void {
    this.authService.verifyGoogleCredential(credential).subscribe({
      next: () => {
        void this.router.navigate([AppRoute.Accounts]);
      },
    });
  }
}
