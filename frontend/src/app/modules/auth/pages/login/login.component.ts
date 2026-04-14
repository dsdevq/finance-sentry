import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {ActivatedRoute, Router, RouterLink} from '@angular/router';
import {
  AlertComponent,
  ButtonComponent,
  FormFieldComponent,
  InputComponent,
} from '@dsdevq-common/ui';

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
    InputComponent,
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  public readonly form = inject(FormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  public errorMessage = '';
  public loading = false;

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

  public onSubmit(): void {
    if (this.form.invalid) {
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    const {email, password} = this.form.value;
    this.authService.login({email: email ?? '', password: password ?? ''}).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/accounts';
        void this.router.navigateByUrl(returnUrl);
      },
      error: () => {
        this.errorMessage = 'Invalid email or password.';
        this.loading = false;
      },
    });
  }
}
