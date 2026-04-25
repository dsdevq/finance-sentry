import {type AbstractControl, type ValidationErrors} from '@angular/forms';

export function passwordsMatch(group: AbstractControl): Nullable<ValidationErrors> {
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
