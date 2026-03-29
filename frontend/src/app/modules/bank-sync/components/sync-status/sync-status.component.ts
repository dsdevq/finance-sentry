import { Component, Input, OnInit, OnDestroy, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BankSyncService, SyncStatusResponse } from '../../services/bank-sync.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-sync-status',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sync-status.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SyncStatusComponent implements OnInit, OnDestroy {
  @Input() accountId!: string;

  syncStatus: SyncStatusResponse | null = null;
  isSyncing = false;
  errorMessage: string | null = null;

  private readonly bankSyncService = inject(BankSyncService);
  private readonly cdr = inject(ChangeDetectorRef);
  private pollSubscription: Subscription | null = null;

  ngOnInit(): void {
    this.loadStatus();
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  loadStatus(): void {
    this.bankSyncService.getSyncStatus(this.accountId).subscribe({
      next: (status) => {
        this.syncStatus = status;
        this.isSyncing = status.status === 'running' || status.status === 'pending';
        this.cdr.markForCheck();
      },
      error: () => { /* ignore initial load errors */ }
    });
  }

  triggerSync(): void {
    this.isSyncing = true;
    this.errorMessage = null;
    this.cdr.markForCheck();

    this.bankSyncService.triggerSync(this.accountId).subscribe({
      next: () => this.startPolling(),
      error: () => {
        this.isSyncing = false;
        this.errorMessage = 'Failed to trigger sync. Please try again.';
        this.cdr.markForCheck();
      }
    });
  }

  private startPolling(): void {
    this.stopPolling();
    this.pollSubscription = this.bankSyncService.pollSyncStatus(this.accountId).subscribe({
      next: (status) => {
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
      }
    });
  }

  stopPolling(): void {
    this.pollSubscription?.unsubscribe();
    this.pollSubscription = null;
  }

  getRelativeTime(timestamp: string | null): string {
    if (!timestamp) return 'Never';
    const diff = Date.now() - new Date(timestamp).getTime();
    const minutes = Math.floor(diff / 60000);
    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes} minute${minutes !== 1 ? 's' : ''} ago`;
    const hours = Math.floor(minutes / 60);
    return `${hours} hour${hours !== 1 ? 's' : ''} ago`;
  }

  getSyncBadgeClass(): Record<string, boolean> {
    return {
      'badge-syncing': this.isSyncing,
      'badge-success': this.syncStatus?.status === 'success',
      'badge-failed': this.syncStatus?.status === 'failed',
      'badge-pending': !this.syncStatus,
    };
  }
}
