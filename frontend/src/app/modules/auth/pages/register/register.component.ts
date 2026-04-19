import {HttpErrorResponse} from '@angular/common/http';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  NgZone,
  OnDestroy,
  viewChild,
} from '@angular/core';
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
  InputComponent,
} from '@dsdevq-common/ui';

import {environment} from '../../../../../environments/environment';
import {AuthService} from '../../services/auth.service';

const MIN_PASSWORD_LENGTH = 8;

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return password === confirm ? null : {passwordsMismatch: true};
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
    InputComponent,
  ],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent implements AfterViewInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly zone = inject(NgZone);
  private readonly googleBtnRef = viewChild.required<ElementRef<HTMLElement>>('googleBtn');

  public readonly form = inject(FormBuilder).group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(MIN_PASSWORD_LENGTH)]],
      confirmPassword: ['', Validators.required],
    },
    {validators: passwordsMatch}
  );
  public errorMessage = '';
  public loading = false;

  public ngAfterViewInit(): void {
    google.accounts.id.initialize({
      // eslint-disable-next-line @typescript-eslint/naming-convention
      client_id: environment.googleClientId,
      callback: (r: google.accounts.id.CredentialResponse) =>
        this.zone.run(() => this.onGoogleCredential(r)),
    });
    google.accounts.id.renderButton(this.googleBtnRef().nativeElement, {
      type: 'standard',
      shape: 'rectangular',
      theme: 'outline',
      text: 'continue_with',
      size: 'large',
      width: 368,
    });
    google.accounts.id.prompt();
  }

  public ngOnDestroy(): void {
    google.accounts.id.cancel();
  }

  public get emailError(): string {
    const ctrl = this.form.get('email');
    if (!ctrl?.touched) {
      return '';
    }
    if (ctrl.hasError('required')) {
      return 'Email is required.';
    }
    if (ctrl.hasError('email')) {
      return 'Enter a valid email address.';
    }
    return '';
  }

  public get passwordError(): string {
    const ctrl = this.form.get('password');
    if (!ctrl?.touched) {
      return '';
    }
    if (ctrl.hasError('required')) {
      return 'Password is required.';
    }
    if (ctrl.hasError('minlength')) {
      return `Password must be at least ${MIN_PASSWORD_LENGTH} characters.`;
    }
    return '';
  }

  public get confirmPasswordError(): string {
    const ctrl = this.form.get('confirmPassword');
    if (!ctrl?.touched) {
      return '';
    }
    if (ctrl.hasError('required')) {
      return 'Please confirm your password.';
    }
    if (this.form.hasError('passwordsMismatch')) {
      return 'Passwords do not match.';
    }
    return '';
  }

  public onSubmit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const {email, password} = this.form.value;
    this.authService.register({email: email ?? '', password: password ?? ''}).subscribe({
      next: () => {
        void this.router.navigate(['/accounts']);
      },
      error: (err: unknown) => {
        const code =
          err instanceof HttpErrorResponse
            ? (err.error as {errorCode?: string} | null)?.errorCode
            : undefined;
        this.errorMessage =
          code === 'DUPLICATE_EMAIL'
            ? 'Email is already registered.'
            : 'Registration failed. Please check your details and try again.';
        this.loading = false;
      },
    });
  }

  private onGoogleCredential(response: google.accounts.id.CredentialResponse): void {
    this.authService.verifyGoogleCredential(response.credential).subscribe({
      next: () => {
        void this.router.navigate(['/accounts']);
      },
      error: () => {
        this.errorMessage = 'Google sign-in failed. Please try again.';
        this.loading = false;
      },
    });
  }
}
