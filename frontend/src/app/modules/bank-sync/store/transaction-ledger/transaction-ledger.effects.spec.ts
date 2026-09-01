import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type DashboardData} from '../../models/dashboard/dashboard.model';
import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {transactionLedgerEffects} from './transaction-ledger.effects';
import {PAGE_SIZE} from './transaction-ledger.state';

// Mirrors the private helper in effects.ts so test data stays in sync with the runtime.
function currentUtcMonthKey(): string {
  const now = new Date();
  return `${now.getUTCFullYear()}-${String(now.getUTCMonth() + 1).padStart(2, '0')}`;
}

function prevUtcMonthKey(): string {
  const now = new Date();
  const prev = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth() - 1, 1));
  return `${prev.getUTCFullYear()}-${String(prev.getUTCMonth() + 1).padStart(2, '0')}`;
}

const TX_ITEM: GlobalTransactionDto = {
  transactionId: 'tx-1',
  accountId: 'acc-1',
  bankName: 'Test Bank',
  currency: 'USD',
  amount: 400,
  amountUsd: 400,
  date: '2026-08-10',
  postedDate: '2026-08-10',
  description: 'Grocery',
  transactionType: 'debit',
  merchantCategory: null,
  isPending: false,
  createdAt: '2026-08-10T00:00:00Z',
};

const TX_RESPONSE = {
  items: [TX_ITEM],
  totalCount: 1,
  offset: 0,
  limit: PAGE_SIZE,
  hasMore: false,
};

function buildDashboardData(currentOutflowUsd: number, priorOutflowUsd?: number): DashboardData {
  const monthlyFlow = [
    {
      month: currentUtcMonthKey(),
      currency: 'USD',
      inflow: 4800,
      outflow: currentOutflowUsd,
      net: 4800 - currentOutflowUsd,
      inflowUsd: 4800,
      outflowUsd: currentOutflowUsd,
      netUsd: 4800 - currentOutflowUsd,
    },
  ];
  if (priorOutflowUsd !== undefined) {
    monthlyFlow.push({
      month: prevUtcMonthKey(),
      currency: 'USD',
      inflow: 5000,
      outflow: priorOutflowUsd,
      net: 5000 - priorOutflowUsd,
      inflowUsd: 5000,
      outflowUsd: priorOutflowUsd,
      netUsd: 5000 - priorOutflowUsd,
    });
  }
  return {
    aggregatedBalance: {USD: 50_000},
    totalNetWorthUsd: 50_000,
    accountCount: 3,
    accountsByType: {banking: 2, brokerage: 1},
    monthlyFlow,
    topCategories: [],
    lastSyncTimestamp: null,
  };
}

// offset is a real Signal<number> (not a mock) so the EffectsStore type constraint is satisfied.
// Pass initialOffset to simulate the state after nextPage() has advanced the cursor.
function buildStore(initialOffset = 0) {
  return {
    offset: signal(initialOffset),
    setLoading: vi.fn(),
    setTransactions: vi.fn(),
    appendTransactions: vi.fn(),
    nextPage: vi.fn(),
    setError: vi.fn(),
    setMonthlyOutflowUsd: vi.fn(),
  };
}

function buildService() {
  return {
    getAllTransactions: vi.fn(),
    getDashboardData: vi.fn(),
  };
}

function configure(service: ReturnType<typeof buildService>): void {
  TestBed.configureTestingModule({
    providers: [{provide: BankSyncService, useValue: service}],
  });
}

describe('transactionLedgerEffects', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  describe('load', () => {
    it('calls both services and sets transactions and current-month outflow', () => {
      const store = buildStore();
      const service = buildService();
      service.getAllTransactions.mockReturnValue(of(TX_RESPONSE));
      service.getDashboardData.mockReturnValue(of(buildDashboardData(2900)));
      configure(service);

      TestBed.runInInjectionContext(() => transactionLedgerEffects(store).load());

      expect(store.setLoading).toHaveBeenCalled();
      expect(service.getAllTransactions).toHaveBeenCalledWith({offset: 0, limit: PAGE_SIZE});
      expect(store.setTransactions).toHaveBeenCalledWith([TX_ITEM], 1, false);
      expect(store.setMonthlyOutflowUsd).toHaveBeenCalledWith(2900);
    });

    it('sums only the current-month outflowUsd and ignores past-month rows', () => {
      // Prior month has 3 000 outflowUsd — must NOT be included.
      const store = buildStore();
      const service = buildService();
      service.getAllTransactions.mockReturnValue(of(TX_RESPONSE));
      service.getDashboardData.mockReturnValue(of(buildDashboardData(2900, 3000)));
      configure(service);

      TestBed.runInInjectionContext(() => transactionLedgerEffects(store).load());

      expect(store.setMonthlyOutflowUsd).toHaveBeenCalledWith(2900); // not 5 900
    });

    it('silences a dashboard error, sets monthlyOutflowUsd to null, keeps ledger loading', () => {
      const store = buildStore();
      const service = buildService();
      service.getAllTransactions.mockReturnValue(of(TX_RESPONSE));
      service.getDashboardData.mockReturnValue(throwError(() => new Error('network error')));
      configure(service);

      TestBed.runInInjectionContext(() => transactionLedgerEffects(store).load());

      // Ledger data must still arrive.
      expect(store.setTransactions).toHaveBeenCalledWith([TX_ITEM], 1, false);
      // Outflow degrades to null, not a hard error.
      expect(store.setMonthlyOutflowUsd).toHaveBeenCalledWith(null);
      expect(store.setError).not.toHaveBeenCalled();
    });

    it('sets monthlyOutflowUsd to 0 when monthlyFlow has no entry for the current month', () => {
      const store = buildStore();
      const service = buildService();
      service.getAllTransactions.mockReturnValue(of(TX_RESPONSE));
      service.getDashboardData.mockReturnValue(
        of({...buildDashboardData(2900), monthlyFlow: []})
      );
      configure(service);

      TestBed.runInInjectionContext(() => transactionLedgerEffects(store).load());

      expect(store.setMonthlyOutflowUsd).toHaveBeenCalledWith(0);
    });
  });

  describe('loadMore', () => {
    it('calls nextPage, then fetches the next page using the updated offset', () => {
      // initialOffset=PAGE_SIZE simulates the state after nextPage() has advanced the cursor.
      // The real nextPage() mutates store state; here it is a vi.fn(), so we seed the offset.
      const store = buildStore(PAGE_SIZE);
      const service = buildService();
      const PAGE2 = {items: [], totalCount: 1, offset: PAGE_SIZE, limit: PAGE_SIZE, hasMore: false};
      service.getAllTransactions.mockReturnValue(of(PAGE2));
      configure(service);

      TestBed.runInInjectionContext(() => transactionLedgerEffects(store).loadMore());

      expect(store.nextPage).toHaveBeenCalled();
      expect(service.getAllTransactions).toHaveBeenCalledWith({
        offset: PAGE_SIZE,
        limit: PAGE_SIZE,
      });
      expect(store.appendTransactions).toHaveBeenCalledWith([], 1, false);
    });
  });
});
