import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {By} from '@angular/platform-browser';
import {CmnDialogService} from '@dsdevq-common/ui';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {
  type AccountBalanceItem,
  type Institution,
} from '../../../../shared/models/wealth/wealth.model';
import {AccountsStore} from '../../../bank-sync/store/accounts/accounts.store';
import {HoldingsStore} from '../../store/holdings.store';
import {HoldingsComponent} from './holdings.component';

const MOCK_ACCOUNT: AccountBalanceItem = {
  accountId: 'acc-1',
  bankName: 'Interactive Brokers',
  provider: 'ibkr',
  category: 'brokerage',
  accountType: 'Margin',
  accountNumberLast4: '7890',
  currentBalance: 12345.67,
  currency: 'USD',
  balanceInBaseCurrency: 12345.67,
  syncStatus: 'synced',
  lastSyncTimestamp: null,
};

const MOCK_INSTITUTION: Institution = {
  institutionId: 'inst-1',
  provider: 'ibkr',
  name: 'Interactive Brokers',
  category: 'brokerage',
  totalInBaseCurrency: 12345.67,
  syncStatus: 'synced',
  lastSyncTimestamp: null,
  accounts: [MOCK_ACCOUNT],
};

describe('HoldingsComponent — cmn-disclosure-row projected slots', () => {
  let mockHoldingsStore: {
    errorMessage: ReturnType<typeof signal<string>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    totalNetWorth: ReturnType<typeof signal<number>>;
    bankingCategory: ReturnType<typeof signal<null>>;
    brokerageCategory: ReturnType<typeof signal<null>>;
    cryptoCategory: ReturnType<typeof signal<null>>;
    allInstitutions: ReturnType<typeof signal<Institution[]>>;
    summary: ReturnType<typeof signal<null>>;
    baseCurrency: ReturnType<typeof signal<string>>;
    isPositionsLoading: ReturnType<typeof signal<boolean>>;
    positionsErrorMessage: ReturnType<typeof signal<string>>;
    totalPositionsValue: ReturnType<typeof signal<number>>;
    positionsByAssetClass: ReturnType<typeof signal<never[]>>;
    positionsLoaded: ReturnType<typeof signal<boolean>>;
    loadPositions: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockHoldingsStore = {
      errorMessage: signal(''),
      isLoading: signal(false),
      totalNetWorth: signal(12345.67),
      bankingCategory: signal(null),
      brokerageCategory: signal(null),
      cryptoCategory: signal(null),
      allInstitutions: signal<Institution[]>([MOCK_INSTITUTION]),
      summary: signal(null),
      baseCurrency: signal('USD'),
      isPositionsLoading: signal(false),
      positionsErrorMessage: signal(''),
      totalPositionsValue: signal(0),
      positionsByAssetClass: signal([]),
      positionsLoaded: signal(false),
      loadPositions: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [HoldingsComponent],
      providers: [{provide: CmnDialogService, useValue: {open: vi.fn()}}],
    })
      .overrideComponent(HoldingsComponent, {
        set: {
          providers: [
            {provide: HoldingsStore, useValue: mockHoldingsStore},
            {
              provide: AccountsStore,
              useValue: {disconnectBinance: vi.fn(), disconnectIBKR: vi.fn()},
            },
          ],
        },
      })
      .compileComponents();
  });

  it('renders cmn-disclosure-row for each institution instead of a raw details element', () => {
    const fixture = TestBed.createComponent(HoldingsComponent);
    fixture.detectChanges();

    const rows = fixture.debugElement.queryAll(By.css('cmn-disclosure-row'));
    expect(rows).toHaveLength(1);
    expect(fixture.debugElement.query(By.css('details.group'))).toBeNull();
  });

  it('[status] slot projects sync-status tag inside the disclosure row', () => {
    const fixture = TestBed.createComponent(HoldingsComponent);
    fixture.detectChanges();

    const tag = fixture.nativeElement.querySelector('cmn-disclosure-row cmn-tag');
    expect(tag).not.toBeNull();
    expect(tag.textContent.trim()).toContain('Synced');
  });

  it('[actions] slot projects disconnect button for a disconnectable provider (ibkr)', () => {
    const fixture = TestBed.createComponent(HoldingsComponent);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('cmn-disclosure-row cmn-button');
    expect(button).not.toBeNull();
    expect(button.textContent.trim()).toContain('Disconnect');
  });

  it('does not render disconnect button when provider is not disconnectable', () => {
    mockHoldingsStore.allInstitutions.set([{...MOCK_INSTITUTION, provider: 'plaid'}]);

    const fixture = TestBed.createComponent(HoldingsComponent);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('cmn-disclosure-row cmn-button');
    expect(button).toBeNull();
  });
});
