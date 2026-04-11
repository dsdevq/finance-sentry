import {CommonModule} from '@angular/common';
import {HttpErrorResponse} from '@angular/common/http';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import {Router, RouterLink} from '@angular/router';

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
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
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
}
