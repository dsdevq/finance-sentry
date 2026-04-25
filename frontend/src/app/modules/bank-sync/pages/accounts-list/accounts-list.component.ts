import {DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {Router} from '@angular/router';
import {AlertComponent, BadgeComponent, ButtonComponent, CardComponent} from '@dsdevq-common/ui';

import {type AccountBalanceItem} from '../../models/wealth.model';
import {AccountsStore} from '../../store/accounts/accounts.store';

const BALANCE_DECIMAL_PLACES = 2;

const BADGE_VARIANT_MAP: Record<string, 'success' | 'warning' | 'error' | 'neutral'> = {
  active: 'success',
  pending: 'warning',
  syncing: 'warning',
  failed: 'error',
  // eslint-disable-next-line @typescript-eslint/naming-convention
  reauth_required: 'error',
};

const BADGE_LABEL_MAP: Record<string, string> = {
  active: 'Synced',
  pending: 'Pending',
  syncing: 'Syncing',
  failed: 'Failed',
  // eslint-disable-next-line @typescript-eslint/naming-convention
  reauth_required: 'Reauth',
};

@Component({
  selector: 'fns-accounts-list',
  standalone: true,
  imports: [
    AlertComponent,
    BadgeComponent,
    ButtonComponent,
    CardComponent,
    DecimalPipe,
  ],
  templateUrl: './accounts-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AccountsStore],
})
export class AccountsListComponent {
  private readonly router = inject(Router);

  public readonly store = inject(AccountsStore);

  public connectAccount(): void {
    void this.router.navigate(['/accounts/connect']);
  }

  public getBadgeVariant(syncStatus: string): 'success' | 'warning' | 'error' | 'neutral' {
    return BADGE_VARIANT_MAP[syncStatus] ?? 'neutral';
  }

  public getBadgeLabel(syncStatus: string): string {
    return BADGE_LABEL_MAP[syncStatus] ?? syncStatus;
  }

  public formatBalance(account: AccountBalanceItem): string {
    const value = account.balanceInBaseCurrency ?? account.currentBalance;
    return `${account.currency} ${value.toFixed(BALANCE_DECIMAL_PLACES)}`;
  }
}
