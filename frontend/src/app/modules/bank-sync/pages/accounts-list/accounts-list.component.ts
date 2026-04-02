import {Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef} from '@angular/core';
import {CommonModule} from '@angular/common';
import {Router} from '@angular/router';
import {BankSyncService} from '../../services/bank-sync.service';
import {BankAccount, SyncStatus} from '../../models/bank-account.model';
import {SyncStatusComponent} from '../../components/sync-status/sync-status.component';

@Component({
  selector: 'app-accounts-list',
  standalone: true,
  imports: [CommonModule, SyncStatusComponent],
  templateUrl: './accounts-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountsListComponent implements OnInit {
  public accounts: BankAccount[] = [];
  public isLoading = false;
  public errorMessage: string | null = null;
  public syncingAccountId: string | null = null;

  public readonly statusLabels: Record<SyncStatus, string> = {
    pending: 'Pending',
    syncing: 'Syncing',
    active: 'Active',
    failed: 'Failed',
    reauth_required: 'Reauth Required',
  };

  public readonly statusClasses: Record<SyncStatus, string> = {
    pending: 'badge-secondary',
    syncing: 'badge-warning',
    active: 'badge-success',
    failed: 'badge-danger',
    reauth_required: 'badge-orange',
  };

  private readonly bankSyncService = inject(BankSyncService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  public ngOnInit(): void {
    this.loadAccounts();
  }

  public loadAccounts(): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.bankSyncService.getAccounts().subscribe({
      next: (res) => {
        this.accounts = res.accounts;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.errorMessage = 'Failed to load accounts. Please try again.';
        this.isLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  public viewTransactions(accountId: string): void {
    void this.router.navigate(['/accounts', accountId, 'transactions']);
  }

  public connectAccount(): void {
    void this.router.navigate(['/accounts/connect']);
  }

  public triggerSync(accountId: string): void {
    this.syncingAccountId = accountId;
    this.cdr.markForCheck();
  }

  public getStatusLabel(status: SyncStatus): string {
    return this.statusLabels[status] ?? status;
  }

  public getStatusClass(status: SyncStatus): string {
    return this.statusClasses[status] ?? 'badge-secondary';
  }
}
