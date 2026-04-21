import {CommonModule} from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  inject,
  OnDestroy,
  OnInit,
} from '@angular/core';
import {FormControl, ReactiveFormsModule, Validators} from '@angular/forms';
import {Router} from '@angular/router';
import {
  AlertComponent,
  ButtonComponent,
  FormFieldComponent,
  InputComponent,
} from '@dsdevq-common/ui';

import {Provider} from '../../models/bank-account.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {
  PlaidHandler,
  PlaidLinkService,
  PlaidSuccessMetadata,
} from '../../services/plaid-link.service';
import {MONOBANK_TOKEN_MAX_LENGTH} from './connect-account.constants';

@Component({
  selector: 'fns-connect-account',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    AlertComponent,
    ButtonComponent,
    FormFieldComponent,
    InputComponent,
  ],
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

  public selectedProvider: Provider = 'plaid';
  public isLoading = false;
  public isSyncing = false;
  public errorMessage: string | null = null;
  public statusMessage: string | null = null;

  public readonly monobankToken = new FormControl<string>('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(MONOBANK_TOKEN_MAX_LENGTH)],
  });

  public ngOnInit(): void {
    void this.initPlaid();
  }

  public ngOnDestroy(): void {
    this.clearPoll();
    this.plaidHandler?.destroy();
  }

  public selectProvider(provider: Provider): void {
    this.selectedProvider = provider;
    this.errorMessage = null;
    this.cdr.markForCheck();
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
            onSuccess: (publicToken, metadata) => this.onPlaidSuccess(publicToken, metadata),
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

  public connectMonobank(): void {
    if (this.monobankToken.invalid) {
      this.monobankToken.markAsTouched();
      return;
    }

    this.isSyncing = true;
    this.errorMessage = null;
    this.statusMessage = 'Connecting your Monobank account...';
    this.cdr.markForCheck();

    this.bankSyncService.connectMonobank(this.monobankToken.value).subscribe({
      next: res => {
        this.statusMessage = `Connected ${res.accounts.length} account(s). Syncing transaction history...`;
        this.cdr.markForCheck();
        this.pollForActiveStatus();
      },
      error: err => {
        this.isSyncing = false;
        const errBody = err as {error?: {errorCode?: string}};
        const code: string = errBody?.error?.errorCode ?? '';
        if (code === 'MONOBANK_TOKEN_INVALID') {
          this.errorMessage = 'Invalid Monobank token. Please check and try again.';
        } else if (code === 'MONOBANK_TOKEN_DUPLICATE') {
          this.errorMessage = 'This Monobank token is already connected.';
        } else if (code === 'MONOBANK_RATE_LIMITED') {
          this.errorMessage = 'Monobank rate limit reached. Please wait 60 seconds and try again.';
        } else {
          this.errorMessage = 'Failed to connect Monobank account. Please try again.';
        }
        this.cdr.markForCheck();
      },
    });
  }

  private onPlaidSuccess(publicToken: string, metadata: PlaidSuccessMetadata): void {
    this.isSyncing = true;
    this.statusMessage = 'Linking your account...';
    this.cdr.markForCheck();

    const institutionName = metadata.institution?.name ?? 'Unknown';
    this.bankSyncService.exchangePublicToken(publicToken, institutionName).subscribe({
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
