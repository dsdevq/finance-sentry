import {CommonModule} from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  inject,
  input,
  OnDestroy,
  OnInit,
} from '@angular/core';
import {Subscription} from 'rxjs';

import {BankSyncService, SyncStatusResponse} from '../../services/bank-sync.service';

const ONE_MINUTE_MS = 60000;
const SECONDS_PER_MINUTE = 60;
const MINUTES_PER_HOUR = 60;

@Component({
  selector: 'fns-sync-status',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sync-status.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SyncStatusComponent implements OnInit, OnDestroy {
  private readonly bankSyncService = inject(BankSyncService);
  private readonly cdr = inject(ChangeDetectorRef);
  private pollSubscription: Subscription | null = null;

  public readonly accountId = input.required<string>();

  public syncStatus: SyncStatusResponse | null = null;
  public isSyncing = false;
  public errorMessage: string | null = null;

  public ngOnInit(): void {
    this.loadStatus();
  }

  public ngOnDestroy(): void {
    this.stopPolling();
  }

  public loadStatus(): void {
    this.bankSyncService.getSyncStatus(this.accountId()).subscribe({
      next: status => {
        this.syncStatus = status;
        this.isSyncing = status.status === 'running' || status.status === 'pending';
        this.cdr.markForCheck();
      },
      error: () => {
        /* ignore initial load errors */
      },
    });
  }

  public triggerSync(): void {
    this.isSyncing = true;
    this.errorMessage = null;
    this.cdr.markForCheck();

    this.bankSyncService.triggerSync(this.accountId()).subscribe({
      next: () => this.startPolling(),
      error: () => {
        this.isSyncing = false;
        this.errorMessage = 'Failed to trigger sync. Please try again.';
        this.cdr.markForCheck();
      },
    });
  }

  public stopPolling(): void {
    this.pollSubscription?.unsubscribe();
    this.pollSubscription = null;
  }

  public getRelativeTime(timestamp: string | null): string {
    if (!timestamp) {
      return 'Never';
    }
    const diff = Date.now() - new Date(timestamp).getTime();
    const minutes = Math.floor(diff / ONE_MINUTE_MS);
    if (minutes < 1) {
      return 'Just now';
    }
    if (minutes < SECONDS_PER_MINUTE) {
      return `${minutes} minute${minutes !== 1 ? 's' : ''} ago`;
    }
    const hours = Math.floor(minutes / MINUTES_PER_HOUR);
    return `${hours} hour${hours !== 1 ? 's' : ''} ago`;
  }

  public getSyncBadgeClass(): Record<string, boolean> {
    return {
      // eslint-disable-next-line @typescript-eslint/naming-convention
      'badge-syncing': this.isSyncing,
      // eslint-disable-next-line @typescript-eslint/naming-convention
      'badge-success': this.syncStatus?.status === 'success',
      // eslint-disable-next-line @typescript-eslint/naming-convention
      'badge-failed': this.syncStatus?.status === 'failed',
      // eslint-disable-next-line @typescript-eslint/naming-convention
      'badge-pending': !this.syncStatus,
    };
  }

  private startPolling(): void {
    this.stopPolling();
    this.pollSubscription = this.bankSyncService.pollSyncStatus(this.accountId()).subscribe({
      next: status => {
        this.syncStatus = status;
        this.isSyncing = status.status === 'running' || status.status === 'pending';
        if (status.status === 'failed') {
          this.errorMessage = status.errorMessage ?? 'Sync failed. Please try again.';
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.isSyncing = false;
        this.errorMessage = 'Sync status check failed.';
        this.cdr.markForCheck();
      },
    });
  }
}
