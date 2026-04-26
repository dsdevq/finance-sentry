import {DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject, ViewContainerRef} from '@angular/core';
import {
  AlertComponent,
  BadgeComponent,
  ButtonComponent,
  CardComponent,
  CmnDialogService,
} from '@dsdevq-common/ui';
import {take} from 'rxjs';

import {SyncStatusLabelPipe} from '../../../../shared/pipes/sync-status-label.pipe';
import {SyncStatusVariantPipe} from '../../../../shared/pipes/sync-status-variant.pipe';
import {ConnectModalComponent} from '../../components/connect-modal/connect-modal.component';
import {DisconnectDialogComponent} from '../../components/disconnect-dialog/disconnect-dialog.component';
import {type BankAccount} from '../../models/bank-account/bank-account.model';
import {AccountBalancePipe} from '../../pipes/account-balance.pipe';
import {AccountsStore} from '../../store/accounts/accounts.store';
import {ConnectStore} from '../../store/connect/connect.store';

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
  providers: [AccountsStore, ConnectStore],
})
export class AccountsListComponent {
  private readonly dialog = inject(CmnDialogService);
  private readonly viewContainerRef = inject(ViewContainerRef);
  private readonly connectStore = inject(ConnectStore);

  public readonly store = inject(AccountsStore);

  public connectAccount(): void {
    this.connectStore.openModal();
    this.dialog.open(ConnectModalComponent, {
      title: 'Connect account',
      size: 'md',
      viewContainerRef: this.viewContainerRef,
    });
  }

  public disconnect(account: Pick<BankAccount, 'accountId' | 'bankName' | 'provider'>): void {
    const ref = this.dialog.open<boolean>(DisconnectDialogComponent, {
      title: `Disconnect ${account.bankName}`,
      size: 'sm',
      viewContainerRef: this.viewContainerRef,
      data: {providerName: account.bankName},
    });
    ref
      .afterClosed()
      .pipe(take(1))
      .subscribe(confirmed => {
        if (confirmed !== true) {
          return;
        }
        if (account.provider === 'monobank') {
          this.store.disconnectMonobank();
          return;
        }
        this.store.disconnectAccount(account.accountId);
      });
  }
}
