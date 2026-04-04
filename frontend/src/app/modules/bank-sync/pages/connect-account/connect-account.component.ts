import {CommonModule} from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  inject,
  OnDestroy,
  OnInit,
} from '@angular/core';
import {Router} from '@angular/router';

import {BankSyncService} from '../../services/bank-sync.service';
import {PlaidHandler, PlaidLinkService} from '../../services/plaid-link.service';

@Component({
  selector: 'fns-connect-account',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './connect-account.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConnectAccountComponent implements OnInit, OnDestroy {
  private readonly bankSyncService = inject(BankSyncService);
  private readonly plaidLinkService = inject(PlaidLinkService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);
  private plaidHandler: PlaidHandler | null = null;
  private pollInterval: ReturnType<typeof setInterval> | null = null;
  private readonly pollMaxMs = 60_000;
  private readonly pollIntervalMs = 3_000;

  public isLoading = false;
  public isSyncing = false;
  public errorMessage: string | null = null;
  public statusMessage: string | null = null;

  public ngOnInit(): void {
    void this.initPlaid();
  }

  public ngOnDestroy(): void {
    this.clearPoll();
    this.plaidHandler?.destroy();
  }

  public async initPlaid(): Promise<void> {
    this.isLoading = true;
    this.errorMessage = null;
    this.cdr.markForCheck();

    try {
      await this.plaidLinkService.loadScript();

      this.bankSyncService.getLinkToken().subscribe({
        next: res => {
          this.plaidHandler = this.plaidLinkService.create({
            token: res.linkToken,
            onSuccess: publicToken => this.onPlaidSuccess(publicToken),
            onExit: err => this.onPlaidExit(err),
          });
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: err => {
          console.error('getLinkToken error:', err);
          this.errorMessage = 'Failed to initialize bank connection. Please try again.';
          this.isLoading = false;
          this.cdr.markForCheck();
        },
      });
    } catch {
      this.errorMessage = 'Failed to load Plaid Link. Please check your connection.';
      this.isLoading = false;
      this.cdr.markForCheck();
    }
  }

  public openPlaidLink(): void {
    this.plaidHandler?.open();
  }

  private onPlaidSuccess(publicToken: string): void {
    this.isSyncing = true;
    this.statusMessage = 'Linking your account...';
    this.cdr.markForCheck();

    this.bankSyncService.exchangePublicToken(publicToken).subscribe({
      next: () => {
        this.statusMessage = 'Account linked! Syncing transaction history...';
        this.cdr.markForCheck();
        this.pollForActiveStatus();
      },
      error: () => {
        this.isSyncing = false;
        this.errorMessage = 'Failed to link account. Please try again.';
        this.cdr.markForCheck();
      },
    });
  }

  private onPlaidExit(err: unknown): void {
    if (err) {
      console.warn('Plaid Link exited with error:', err);
    }
  }

  private pollForActiveStatus(): void {
    const startTime = Date.now();

    this.pollInterval = setInterval(() => {
      if (Date.now() - startTime > this.pollMaxMs) {
        this.clearPoll();
        void this.router.navigate(['/accounts']);
        return;
      }

      this.bankSyncService.getAccounts().subscribe({
        next: res => {
          const isActive = res.accounts.some(a => a.syncStatus === 'active');
          if (isActive) {
            this.clearPoll();
            void this.router.navigate(['/accounts']);
          }
        },
      });
    }, this.pollIntervalMs);
  }

  private clearPoll(): void {
    if (this.pollInterval !== null) {
      clearInterval(this.pollInterval);
      this.pollInterval = null;
    }
  }
}
