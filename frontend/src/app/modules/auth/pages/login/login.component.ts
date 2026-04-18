import {ChangeDetectionStrategy, Component, inject, OnInit} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {
  AlertComponent,
  ButtonComponent,
  FormFieldComponent,
  InputComponent,
} from '@dsdevq-common/ui';

import {AuthService} from '../../services/auth.service';

interface ApiError {
  error: {errorCode?: string};
}

@Component({
  selector: 'fns-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    AlertComponent,
    ButtonComponent,
    FormFieldComponent,
    InputComponent,
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  public readonly form = inject(FormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  public errorMessage = '';
  public infoMessage = '';
  public loading = false;

  public ngOnInit(): void {
    const params = this.route.snapshot.queryParams as Record<string, string>;
    if (params['info'] === 'google_cancelled') {
      this.infoMessage = 'Google sign-in was cancelled. Try again or use email/password.';
    } else if (params['error'] === 'google_failed') {
      this.errorMessage = 'Google sign-in failed. Please try again.';
    }
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
    return '';
  }

  public googleLogin(): void {
    this.authService.googleLogin();
  }

  public onSubmit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.infoMessage = '';

    const {email, password} = this.form.value;
    this.authService.login({email: email ?? '', password: password ?? ''}).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/accounts';
        void this.router.navigateByUrl(returnUrl);
      },
      error: (err: ApiError) => {
        if (err?.error?.errorCode === 'GOOGLE_ACCOUNT_ONLY') {
          this.errorMessage =
            "This account uses Google sign-in. Click 'Continue with Google' instead.";
        } else {
          this.errorMessage = 'Invalid email or password.';
        }
        this.loading = false;
      },
    });
  }
}
