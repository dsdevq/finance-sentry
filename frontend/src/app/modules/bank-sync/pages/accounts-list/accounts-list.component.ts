import {
  Component,
  OnInit,
  inject,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { BankSyncService } from '../../services/bank-sync.service';
import { BankAccount, SyncStatus } from '../../models/bank-account.model';

@Component({
  selector: 'app-accounts-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './accounts-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountsListComponent implements OnInit {
  private readonly bankSyncService = inject(BankSyncService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  accounts: BankAccount[] = [];
  isLoading = false;
  errorMessage: string | null = null;

  readonly statusLabels: Record<SyncStatus, string> = {
    pending: 'Pending',
    syncing: 'Syncing',
    active: 'Active',
    failed: 'Failed',
    reauth_required: 'Reauth Required',
  };

  readonly statusClasses: Record<SyncStatus, string> = {
    pending: 'badge-secondary',
    syncing: 'badge-warning',
    active: 'badge-success',
    failed: 'badge-danger',
    reauth_required: 'badge-orange',
  };

  ngOnInit(): void {
    this.loadAccounts();
  }

  loadAccounts(): void {
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

  viewTransactions(accountId: string): void {
    void this.router.navigate(['/accounts', accountId, 'transactions']);
  }

  connectAccount(): void {
    void this.router.navigate(['/accounts/connect']);
  }

  getStatusLabel(status: SyncStatus): string {
    return this.statusLabels[status] ?? status;
  }

  getStatusClass(status: SyncStatus): string {
    return this.statusClasses[status] ?? 'badge-secondary';
  }
}
