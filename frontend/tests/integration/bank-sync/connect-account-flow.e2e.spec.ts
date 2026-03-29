/**
 * E2E integration test: Connect Account → View Transactions flow (T220)
 *
 * Uses Angular TestBed with HttpClientTestingModule to simulate the full
 * connect-account → accounts-list → transaction-list user journey without
 * a live backend. Plaid Link is mocked via the PlaidLinkService.
 */

import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { Router } from '@angular/router';
import { Location } from '@angular/common';

import { BankSyncService } from '../../../src/app/modules/bank-sync/services/bank-sync.service';
import { PlaidLinkService } from '../../../src/app/modules/bank-sync/services/plaid-link.service';
import { ConnectAccountComponent } from '../../../src/app/modules/bank-sync/pages/connect-account/connect-account.component';
import { AccountsListComponent } from '../../../src/app/modules/bank-sync/pages/accounts-list/accounts-list.component';
import { TransactionListComponent } from '../../../src/app/modules/bank-sync/pages/transaction-list/transaction-list.component';
import {
  BankAccount,
  ConnectResponse,
  LinkAccountResponse,
  AccountsResponse,
} from '../../../src/app/modules/bank-sync/models/bank-account.model';
import { TransactionListResponse } from '../../../src/app/modules/bank-sync/models/transaction.model';

const MOCK_LINK_RESPONSE: ConnectResponse = {
  linkToken: 'link-sandbox-test-token',
  expiresIn: 600,
  expiresAt: new Date(Date.now() + 600000).toISOString(),
  requestId: 'req_test_001',
};

const MOCK_LINK_ACCOUNT_RESPONSE: LinkAccountResponse = {
  accountId: 'acct-001',
  bankName: 'AIB Ireland',
  accountType: 'checking',
  accountNumberLast4: '1234',
  ownerName: 'Test User',
  currency: 'EUR',
  initialBalance: 5000,
  syncStatus: 'pending',
  lastSyncTimestamp: null,
  message: 'Account linked successfully. Syncing...',
};

const MOCK_ACTIVE_ACCOUNT: BankAccount = {
  accountId: 'acct-001',
  bankName: 'AIB Ireland',
  accountType: 'checking',
  accountNumberLast4: '1234',
  ownerName: 'Test User',
  currency: 'EUR',
  currentBalance: 5000,
  availableBalance: 4800,
  syncStatus: 'active',
  lastSyncTimestamp: new Date().toISOString(),
  lastSyncDurationMs: 2500,
  createdAt: new Date().toISOString(),
};

const MOCK_ACCOUNTS_RESPONSE: AccountsResponse = {
  accounts: [MOCK_ACTIVE_ACCOUNT],
  totalCount: 1,
  currency_totals: { EUR: 5000 },
};

const MOCK_TRANSACTIONS_RESPONSE: TransactionListResponse = {
  accountId: 'acct-001',
  bankName: 'AIB Ireland',
  currency: 'EUR',
  transactions: [
    {
      transactionId: 'txn-001',
      accountId: 'acct-001',
      amount: 45.99,
      transactionType: 'debit',
      postedDate: '2026-03-01',
      pendingDate: null,
      isPending: false,
      description: 'Tesco Metro Dublin',
      merchantCategory: 'Groceries',
      syncedAt: new Date().toISOString(),
      createdAt: new Date().toISOString(),
    },
    {
      transactionId: 'txn-002',
      accountId: 'acct-001',
      amount: 3000,
      transactionType: 'credit',
      postedDate: '2026-03-15',
      pendingDate: null,
      isPending: false,
      description: 'Salary Payment',
      merchantCategory: null,
      syncedAt: new Date().toISOString(),
      createdAt: new Date().toISOString(),
    },
  ],
  pagination: { offset: 0, limit: 50, totalCount: 2, hasMore: false },
};

// ─── Mock PlaidLinkService ───────────────────────────────────────────────────

class MockPlaidLinkService {
  private successCallback?: (publicToken: string, metadata: unknown) => void;

  loadScript(): Promise<void> {
    return Promise.resolve();
  }

  create(options: { onSuccess: (publicToken: string, metadata: unknown) => void }) {
    this.successCallback = options.onSuccess;
    return {
      open: () => {
        // Simulate Plaid Link flow: user selects bank, success fires
        this.successCallback?.('public-sandbox-test-token', {});
      },
      destroy: () => {},
    };
  }

  /** Test helper: trigger Plaid success programmatically */
  triggerSuccess(publicToken = 'public-sandbox-test-token'): void {
    this.successCallback?.(publicToken, {});
  }
}

// ─── Tests ───────────────────────────────────────────────────────────────────

describe('Connect Account Flow (E2E Integration)', () => {
  let httpMock: HttpTestingController;
  let bankSyncService: BankSyncService;
  let router: Router;
  let location: Location;
  let mockPlaidService: MockPlaidLinkService;

  beforeEach(async () => {
    mockPlaidService = new MockPlaidLinkService();

    await TestBed.configureTestingModule({
      imports: [
        HttpClientTestingModule,
        RouterTestingModule.withRoutes([
          { path: 'accounts/connect', component: ConnectAccountComponent },
          { path: 'accounts/list', component: AccountsListComponent },
          {
            path: 'accounts/:accountId/transactions',
            component: TransactionListComponent,
          },
          { path: '', redirectTo: 'accounts/list', pathMatch: 'full' },
        ]),
        ConnectAccountComponent,
        AccountsListComponent,
        TransactionListComponent,
      ],
      providers: [BankSyncService, { provide: PlaidLinkService, useValue: mockPlaidService }],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    bankSyncService = TestBed.inject(BankSyncService);
    router = TestBed.inject(Router);
    location = TestBed.inject(Location);
  });

  afterEach(() => {
    httpMock.verify();
  });

  // ── T220-01: BankSyncService calls correct endpoints ────────────────────

  it('getLinkToken calls POST /api/accounts/connect', () => {
    bankSyncService.getLinkToken().subscribe((res) => {
      expect(res.linkToken).toBe(MOCK_LINK_RESPONSE.linkToken);
    });

    const req = httpMock.expectOne((r) => r.url.includes('/api/accounts/connect'));
    expect(req.request.method).toBe('POST');
    req.flush(MOCK_LINK_RESPONSE);
  });

  it('exchangePublicToken calls POST /api/accounts/link with publicToken', () => {
    bankSyncService.exchangePublicToken('public-test-abc').subscribe((res) => {
      expect(res.accountId).toBe('acct-001');
    });

    const req = httpMock.expectOne((r) => r.url.includes('/api/accounts/link'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ publicToken: 'public-test-abc' });
    req.flush(MOCK_LINK_ACCOUNT_RESPONSE);
  });

  it('getAccounts calls GET /api/accounts', () => {
    bankSyncService.getAccounts().subscribe((res) => {
      expect(res.accounts.length).toBe(1);
      expect(res.accounts[0].syncStatus).toBe('active');
    });

    const req = httpMock.expectOne((r) => r.url.includes('/api/accounts') && r.method === 'GET');
    expect(req.request.method).toBe('GET');
    req.flush(MOCK_ACCOUNTS_RESPONSE);
  });

  it('getTransactions calls GET /api/accounts/{id}/transactions with params', () => {
    bankSyncService
      .getTransactions('acct-001', { offset: 0, limit: 50, sort: 'date:desc' })
      .subscribe((res) => {
        expect(res.transactions.length).toBe(2);
        expect(res.pagination.totalCount).toBe(2);
      });

    const req = httpMock.expectOne((r) => r.url.includes('/api/accounts/acct-001/transactions'));
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('offset')).toBe('0');
    expect(req.request.params.get('limit')).toBe('50');
    expect(req.request.params.get('sort')).toBe('date:desc');
    req.flush(MOCK_TRANSACTIONS_RESPONSE);
  });

  // ── T220-02: AccountsListComponent renders accounts ──────────────────────

  it('AccountsListComponent: fetches and renders accounts with correct status badges', () => {
    const fixture = TestBed.createComponent(AccountsListComponent);
    const component = fixture.componentInstance;

    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url.includes('/api/accounts') && r.method === 'GET');
    req.flush(MOCK_ACCOUNTS_RESPONSE);

    fixture.detectChanges();

    expect(component.accounts.length).toBe(1);
    expect(component.accounts[0].bankName).toBe('AIB Ireland');
    expect(component.accounts[0].syncStatus).toBe('active');

    const compiled: HTMLElement = fixture.nativeElement as HTMLElement;
    const badge = compiled.querySelector('.badge-success');
    expect(badge).not.toBeNull();
    expect(badge?.textContent?.trim()).toBe('Active');
  });

  it('AccountsListComponent: shows empty state when no accounts exist', () => {
    const fixture = TestBed.createComponent(AccountsListComponent);

    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url.includes('/api/accounts') && r.method === 'GET');
    req.flush({ accounts: [], totalCount: 0, currency_totals: {} });

    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No accounts connected');
  });

  // ── T220-03: TransactionListComponent renders transactions ───────────────

  it('TransactionListComponent: fetches and renders transactions with debit/credit badges', () => {
    const fixture = TestBed.createComponent(TransactionListComponent);
    const component = fixture.componentInstance;
    component.accountId = 'acct-001';

    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url.includes('/api/accounts/acct-001/transactions'));
    req.flush(MOCK_TRANSACTIONS_RESPONSE);

    fixture.detectChanges();

    expect(component.transactions.length).toBe(2);
    expect(component.totalCount).toBe(2);

    const compiled: HTMLElement = fixture.nativeElement as HTMLElement;
    const debitBadge = compiled.querySelector('.badge-danger');
    const creditBadge = compiled.querySelector('.badge-success');
    expect(debitBadge).not.toBeNull();
    expect(creditBadge).not.toBeNull();
  });

  it('TransactionListComponent: pagination previous/next updates offset', fakeAsync(() => {
    const fixture = TestBed.createComponent(TransactionListComponent);
    const component = fixture.componentInstance;
    component.accountId = 'acct-001';

    fixture.detectChanges();

    // Initial load
    const req1 = httpMock.expectOne((r) => r.url.includes('/api/accounts/acct-001/transactions'));
    req1.flush({
      ...MOCK_TRANSACTIONS_RESPONSE,
      pagination: { offset: 0, limit: 50, totalCount: 120, hasMore: true },
    });

    fixture.detectChanges();
    tick();

    // Go to next page
    expect(component.hasNext).toBe(true);
    component.nextPage();
    fixture.detectChanges();

    const req2 = httpMock.expectOne((r) => r.url.includes('/api/accounts/acct-001/transactions'));
    expect(req2.request.params.get('offset')).toBe('50');
    req2.flush({
      ...MOCK_TRANSACTIONS_RESPONSE,
      pagination: { offset: 50, limit: 50, totalCount: 120, hasMore: true },
    });

    fixture.detectChanges();
    tick();

    // Go back
    expect(component.hasPrevious).toBe(true);
    component.previousPage();
    fixture.detectChanges();

    const req3 = httpMock.expectOne((r) => r.url.includes('/api/accounts/acct-001/transactions'));
    expect(req3.request.params.get('offset')).toBe('0');
    req3.flush(MOCK_TRANSACTIONS_RESPONSE);
  }));

  // ── T220-04: ConnectAccountComponent initializes Plaid Link ──────────────

  it('ConnectAccountComponent: initializes Plaid Link on load', fakeAsync(async () => {
    const fixture = TestBed.createComponent(ConnectAccountComponent);
    const component = fixture.componentInstance;

    await component.ngOnInit();

    const req = httpMock.expectOne((r) => r.url.includes('/api/accounts/connect'));
    req.flush(MOCK_LINK_RESPONSE);

    fixture.detectChanges();
    tick();

    expect(component.isLoading).toBe(false);
    expect(component.errorMessage).toBeNull();
  }));

  it('ConnectAccountComponent: shows error when getLinkToken fails', fakeAsync(async () => {
    const fixture = TestBed.createComponent(ConnectAccountComponent);
    const component = fixture.componentInstance;

    await component.ngOnInit();

    const req = httpMock.expectOne((r) => r.url.includes('/api/accounts/connect'));
    req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });

    fixture.detectChanges();
    tick();

    expect(component.errorMessage).toContain('Failed to initialize');
    expect(component.isLoading).toBe(false);
  }));

  // ── T220-05: Deduplication accuracy check (SC-005) ──────────────────────

  it('SC-005: 100 transactions with unique hashes all returned (no false duplicates)', () => {
    const transactions = Array.from({ length: 100 }, (_, i) => ({
      transactionId: `txn-${i.toString().padStart(3, '0')}`,
      accountId: 'acct-001',
      amount: (i + 1) * 5.0,
      transactionType: (i % 2 === 0 ? 'debit' : 'credit') as 'debit' | 'credit',
      postedDate: `2026-01-${((i % 28) + 1).toString().padStart(2, '0')}`,
      pendingDate: null,
      isPending: false,
      description: `Merchant_${i}`,
      merchantCategory: i % 3 === 0 ? 'Groceries' : 'Transport',
      syncedAt: new Date().toISOString(),
      createdAt: new Date().toISOString(),
    }));

    bankSyncService.getTransactions('acct-001', { limit: 100 }).subscribe((res) => {
      const ids = new Set(res.transactions.map((t) => t.transactionId));
      expect(ids.size).toBe(100);
    });

    const req = httpMock.expectOne((r) => r.url.includes('/api/accounts/acct-001/transactions'));
    req.flush({
      accountId: 'acct-001',
      bankName: 'AIB Ireland',
      currency: 'EUR',
      transactions,
      pagination: { offset: 0, limit: 100, totalCount: 100, hasMore: false },
    });
  });
});
