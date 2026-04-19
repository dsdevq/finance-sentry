import {ChangeDetectionStrategy, Component} from '@angular/core';
import {FormsModule, FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import type {Meta, StoryObj} from '@storybook/angular';

import {ButtonComponent} from '../button/button.component';
import {InputComponent} from '../input/input.component';
import {FormFieldComponent} from './form-field.component';

const MIN_PASSWORD_LENGTH = 8;

const meta: Meta = {
  title: 'Components/FormField',
  component: FormFieldComponent,
};

export default meta;
type Story = StoryObj<typeof meta>;

const FORM_IMPORTS = [FormFieldComponent, InputComponent];

export const Default: Story = {
  render: () => ({
    moduleMetadata: {imports: FORM_IMPORTS},
    template: `
      <cmn-form-field label="Email">
        <cmn-input type="email" placeholder="you@example.com" />
      </cmn-form-field>
    `,
  }),
};

export const WithHint: Story = {
  render: () => ({
    moduleMetadata: {imports: FORM_IMPORTS},
    template: `
      <cmn-form-field label="Email" hint="We will never share your email with anyone.">
        <cmn-input type="email" placeholder="you@example.com" />
      </cmn-form-field>
    `,
  }),
};

export const WithError: Story = {
  render: () => ({
    moduleMetadata: {imports: FORM_IMPORTS},
    template: `
      <cmn-form-field label="Email" errorMessage="Please enter a valid email address.">
        <cmn-input type="email" [hasError]="true" placeholder="you@example.com" />
      </cmn-form-field>
    `,
  }),
};

export const Required: Story = {
  render: () => ({
    moduleMetadata: {imports: FORM_IMPORTS},
    template: `
      <cmn-form-field label="Password" [required]="true" hint="At least 8 characters.">
        <cmn-input type="password" placeholder="••••••••" />
      </cmn-form-field>
    `,
  }),
};

export const Disabled: Story = {
  render: () => ({
    moduleMetadata: {imports: [...FORM_IMPORTS, FormsModule]},
    template: `
      <cmn-form-field label="Username">
        <cmn-input type="text" [disabled]="true" [ngModel]="'john.doe'" />
      </cmn-form-field>
    `,
  }),
};

@Component({
  selector: 'cmn-form-example',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormFieldComponent, InputComponent, ButtonComponent, ReactiveFormsModule],
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-cmn-4 max-w-sm">
      <cmn-form-field
        [required]="true"
        [errorMessage]="emailError"
        [hint]="emailError ? '' : 'Use your work email'"
        label="Email"
      >
        <cmn-input
          [hasError]="!!emailError"
          formControlName="email"
          type="email"
          placeholder="you@example.com"
        />
      </cmn-form-field>
      <cmn-form-field [required]="true" [errorMessage]="passwordError" label="Password">
        <cmn-input
          [hasError]="!!passwordError"
          formControlName="password"
          type="password"
          placeholder="••••••••"
        />
      </cmn-form-field>
      <cmn-button type="submit" variant="primary">Sign In</cmn-button>
    </form>
  `,
})
class FormExampleComponent {
  public form = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(MIN_PASSWORD_LENGTH)]),
  });

  public get emailError(): string {
    const ctrl = this.form.get('email');
    if (!ctrl?.touched || ctrl.valid) {
      return '';
    }
    if (ctrl.errors?.['required']) {
      return 'Email is required';
    }
    if (ctrl.errors?.['email']) {
      return 'Enter a valid email';
    }
    return '';
  }

  public get passwordError(): string {
    const ctrl = this.form.get('password');
    if (!ctrl?.touched || ctrl.valid) {
      return '';
    }
    if (ctrl.errors?.['required']) {
      return 'Password is required';
    }
    if (ctrl.errors?.['minlength']) {
      return 'At least 8 characters required';
    }
    return '';
  }

  public onSubmit(): void {
    this.form.markAllAsTouched();
  }
}

export const FullReactiveFormExample: Story = {
  render: () => ({
    props: {},
    moduleMetadata: {imports: [FormExampleComponent]},
    template: '<cmn-form-example />',
  }),
};

@Component({
  selector: 'cmn-control-input-example',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormFieldComponent, InputComponent, ButtonComponent, ReactiveFormsModule],
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-cmn-4 max-w-sm">
      <p class="text-cmn-sm text-text-secondary">
        No getter methods — errors auto-resolved from the registry via <code>[control]</code> input.
      </p>
      <cmn-form-field
        [required]="true"
        [control]="form.get('email')"
        label="Email"
      >
        <cmn-input formControlName="email" type="email" placeholder="you@example.com" />
      </cmn-form-field>
      <cmn-form-field [required]="true" [control]="form.get('password')" label="Password">
        <cmn-input formControlName="password" type="password" placeholder="••••••••" />
      </cmn-form-field>
      <cmn-button type="submit" variant="primary">Sign In</cmn-button>
    </form>
  `,
})
class ControlInputExampleComponent {
  public form = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(MIN_PASSWORD_LENGTH)]),
  });

  public onSubmit(): void {
    this.form.markAllAsTouched();
  }
}

export const WithControlInput: Story = {
  name: 'With [control] input (auto-resolved errors)',
  render: () => ({
    props: {},
    moduleMetadata: {imports: [ControlInputExampleComponent]},
    template: '<cmn-control-input-example />',
  }),
};
