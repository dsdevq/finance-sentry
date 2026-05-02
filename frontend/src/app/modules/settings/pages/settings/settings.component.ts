import {ChangeDetectionStrategy, Component, inject, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Router} from '@angular/router';
import {
  AlertComponent,
  BadgeComponent,
  ButtonComponent,
  CardComponent,
  FormFieldComponent,
  InputComponent,
  ToastService,
  ToggleComponent,
} from '@dsdevq-common/ui';

import {AppRoute} from '../../../../shared/enums/app-route/app-route.enum';
import {type BaseCurrency, type ThemePreference} from '../../models/settings/settings.model';
import {SettingsStore} from '../../store/settings/settings.store';

const CURRENCY_OPTIONS: {value: BaseCurrency; label: string}[] = [
  {value: 'USD', label: 'USD — US Dollar'},
  {value: 'EUR', label: 'EUR — Euro'},
  {value: 'GBP', label: 'GBP — British Pound'},
  {value: 'UAH', label: 'UAH — Ukrainian Hryvnia'},
  {value: 'BTC', label: 'BTC — Bitcoin'},
];

const THEME_OPTIONS: {value: ThemePreference; label: string}[] = [
  {value: 'system', label: 'System default'},
  {value: 'light', label: 'Light'},
  {value: 'dark', label: 'Dark'},
];

const PROFILE_SAVE_DELAY_MS = 900;
const PASSWORD_SAVE_DELAY_MS = 1100;
const MIN_PASSWORD_LENGTH = 8;

@Component({
  selector: 'fns-settings',
  imports: [
    ButtonComponent,
    CardComponent,
    FormFieldComponent,
    FormsModule,
    InputComponent,
    ToggleComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SettingsStore],
  templateUrl: './settings.component.html',
})
export class SettingsComponent {
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  public readonly store = inject(SettingsStore);
  public readonly currencyOptions = CURRENCY_OPTIONS;
  public readonly themeOptions = THEME_OPTIONS;

  public readonly pwCurrent = signal('');
  public readonly pwNext = signal('');
  public readonly pwConfirm = signal('');

  public saveProfile(): void {
    this.store.setProfileSaving(true);
    setTimeout(() => {
      this.store.setProfileSaving(false);
      this.toast.show('Profile saved successfully', 'success');
    }, PROFILE_SAVE_DELAY_MS);
  }

  public changePassword(): void {
    if (!this.pwCurrent()) {
      this.toast.show('Enter your current password', 'error');
      return;
    }
    if (this.pwNext().length < MIN_PASSWORD_LENGTH) {
      this.toast.show('New password must be at least 8 characters', 'error');
      return;
    }
    if (this.pwNext() !== this.pwConfirm()) {
      this.toast.show('Passwords do not match', 'error');
      return;
    }
    this.store.setPasswordSaving(true);
    setTimeout(() => {
      this.store.setPasswordSaving(false);
      this.pwCurrent.set('');
      this.pwNext.set('');
      this.pwConfirm.set('');
      this.toast.show('Password updated', 'success');
    }, PASSWORD_SAVE_DELAY_MS);
  }

  public signOut(): void {
    void this.router.navigateByUrl(AppRoute.Login);
  }

  public deleteAccount(): void {
    this.store.setShowDeleteConfirm(false);
    this.toast.show('Account deletion requested', 'warning');
  }
}
