import {DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {Router} from '@angular/router';
import {AlertComponent, BadgeComponent, ButtonComponent, CardComponent} from '@dsdevq-common/ui';

import {SyncStatusLabelPipe} from '../../../../shared/pipes/sync-status-label.pipe';
import {SyncStatusVariantPipe} from '../../../../shared/pipes/sync-status-variant.pipe';
import {AccountBalancePipe} from '../../pipes/account-balance.pipe';
import {AccountsStore} from '../../store/accounts/accounts.store';

@Component({
  selector: 'fns-accounts-list',
  imports: [
    AccountBalancePipe,
    AlertComponent,
    BadgeComponent,
    ButtonComponent,
    CardComponent,
    DecimalPipe,
    SyncStatusLabelPipe,
    SyncStatusVariantPipe,
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
}
