import {InjectionToken} from '@angular/core';

export type ValidationMessageFn = (params?: unknown) => string;

export const DEFAULT_VALIDATION_MESSAGES: Record<string, ValidationMessageFn> = {
  required: () => 'This field is required.',
  email: () => 'Enter a valid email address.',
  minlength: (p: unknown) =>
    `Minimum ${(p as {requiredLength: number}).requiredLength} characters.`,
  maxlength: (p: unknown) =>
    `Maximum ${(p as {requiredLength: number}).requiredLength} characters.`,
  passwordsMismatch: () => 'Passwords do not match.',
};

export const VALIDATION_MESSAGES = new InjectionToken<Record<string, ValidationMessageFn>>(
  'VALIDATION_MESSAGES',
  {
    providedIn: 'root',
    factory: () => DEFAULT_VALIDATION_MESSAGES,
  }
);
