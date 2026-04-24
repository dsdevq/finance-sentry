import {DatePipe, DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {Router} from '@angular/router';
import {AlertComponent, ButtonComponent, CardComponent} from '@dsdevq-common/ui';

import {SyncStatusComponent} from '../../components/sync-status/sync-status.component';
import {type SyncStatus} from '../../models/bank-account.model';
import {AccountsStore} from '../../store/accounts/accounts.store';

const STATUS_LABELS: Record<SyncStatus, string> = {
  pending: 'Pending',
  syncing: 'Syncing',
  active: 'Active',
  failed: 'Failed',
  // eslint-disable-next-line @typescript-eslint/naming-convention
  reauth_required: 'Reauth Required',
};

const STATUS_CLASSES: Record<SyncStatus, string> = {
  pending: 'badge-secondary',
  syncing: 'badge-warning',
  active: 'badge-success',
  failed: 'badge-danger',
  // eslint-disable-next-line @typescript-eslint/naming-convention
  reauth_required: 'badge-orange',
};

@Component({
  selector: 'fns-accounts-list',
  standalone: true,
  imports: [
    AlertComponent,
    ButtonComponent,
    CardComponent,
    DatePipe,
    DecimalPipe,
    SyncStatusComponent,
  ],
  templateUrl: './accounts-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AccountsStore],
})
export class AccountsListComponent {
  private readonly router = inject(Router);

  public readonly store = inject(AccountsStore);

  public viewTransactions(accountId: string): void {
    void this.router.navigate(['/accounts', accountId, 'transactions']);
  }

  public connectAccount(): void {
    void this.router.navigate(['/accounts/connect']);
  }

  public getStatusLabel(status: SyncStatus): string {
    return STATUS_LABELS[status] ?? status;
  }

  public getStatusClass(status: SyncStatus): string {
    return STATUS_CLASSES[status] ?? 'badge-secondary';
  }
}
