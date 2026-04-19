import {ChangeDetectionStrategy, Component, inject, OnInit} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
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
export class LoginComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  public readonly form = inject(FormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  public readonly googleClientId = environment.googleClientId;
  public readonly AppRoute = AppRoute;
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
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? AppRoute.Accounts;
        void this.router.navigateByUrl(returnUrl);
      },
      error: (err: unknown) => {
        const errorCode = (err as {error?: {errorCode?: string}} | null)?.error?.errorCode;
        this.errorMessage =
          errorCode === 'GOOGLE_ACCOUNT_ONLY'
            ? "This account uses Google sign-in. Click 'Continue with Google' instead."
            : 'Invalid email or password.';
        this.loading = false;
      },
    });
  }

  public onGoogleCredential(credential: string): void {
    this.authService.verifyGoogleCredential(credential).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? AppRoute.Accounts;
        void this.router.navigateByUrl(returnUrl);
      },
    });
  }
}
