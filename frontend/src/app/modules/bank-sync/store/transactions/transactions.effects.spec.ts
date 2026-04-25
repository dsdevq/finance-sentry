import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {of, throwError} from 'rxjs';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type TransactionListResponse} from '../../models/transaction/transaction.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {transactionsEffects} from './transactions.effects';
import {PAGE_SIZE} from './transactions.state';

const SAMPLE_RESPONSE: TransactionListResponse = {
  accountId: 'a1',
  bankName: 'Chase',
  currency: 'USD',
  transactions: [],
  pagination: {offset: 0, limit: PAGE_SIZE, totalCount: 0, hasMore: false},
};

function buildStore(accountId: string) {
  return {
    accountId: signal(accountId),
    startDate: signal(''),
    endDate: signal(''),
    offset: signal(0),
    setLoading: vi.fn(),
    setResponse: vi.fn(),
    setError: vi.fn(),
  };
}

function buildService() {
  return {getTransactions: vi.fn()};
}

function configure(service: ReturnType<typeof buildService>) {
  TestBed.configureTestingModule({
    providers: [{provide: BankSyncService, useValue: service}],
  });
}

describe('transactionsEffects', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('load: skips when accountId is empty', () => {
    const store = buildStore('');
    const service = buildService();
    configure(service);

    TestBed.runInInjectionContext(() => transactionsEffects(store).load());

    expect(store.setLoading).toHaveBeenCalled();
    expect(service.getTransactions).not.toHaveBeenCalled();
    expect(store.setResponse).not.toHaveBeenCalled();
  });

  it('load: calls service with current filters and stores response', () => {
    const store = buildStore('a1');
    store.startDate.set('2025-01-01');
    store.endDate.set('2025-01-31');
    store.offset.set(PAGE_SIZE);
    const service = buildService();
    service.getTransactions.mockReturnValue(of(SAMPLE_RESPONSE));
    configure(service);

    TestBed.runInInjectionContext(() => transactionsEffects(store).load());

    expect(service.getTransactions).toHaveBeenCalledWith('a1', {
      offset: PAGE_SIZE,
      limit: PAGE_SIZE,
      startDate: '2025-01-01',
      endDate: '2025-01-31',
      sort: 'date:desc',
    });
    expect(store.setResponse).toHaveBeenCalledWith(SAMPLE_RESPONSE);
  });

  it('load: error path extracts errorCode', () => {
    const store = buildStore('a1');
    const service = buildService();
    service.getTransactions.mockReturnValue(throwError(() => ({error: {errorCode: 'TXN_DOWN'}})));
    configure(service);

    TestBed.runInInjectionContext(() => transactionsEffects(store).load());

    expect(store.setError).toHaveBeenCalledWith('TXN_DOWN');
  });
});
