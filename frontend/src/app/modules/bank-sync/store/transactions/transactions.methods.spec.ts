import {signalState} from '@ngrx/signals';
import {describe, expect, it} from 'vitest';

import {type TransactionListResponse} from '../../models/transaction/transaction.model';
import {transactionsMethods} from './transactions.methods';
import {initialTransactionsState, PAGE_SIZE} from './transactions.state';

const SAMPLE_RESPONSE: TransactionListResponse = {
  accountId: 'a1',
  bankName: 'Chase',
  currency: 'USD',
  transactions: [],
  pagination: {offset: 0, limit: PAGE_SIZE, totalCount: 120, hasMore: true},
};

describe('transactionsMethods', () => {
  it('setAccountId resets offset', () => {
    const state = signalState(initialTransactionsState);
    const methods = transactionsMethods(state);
    methods.setOffset(PAGE_SIZE);

    methods.setAccountId('a1');

    expect(state.accountId()).toBe('a1');
    expect(state.offset()).toBe(0);
  });

  it('setDateRange stores range and resets offset', () => {
    const state = signalState(initialTransactionsState);
    const methods = transactionsMethods(state);
    methods.setOffset(PAGE_SIZE);

    methods.setDateRange('2025-01-01', '2025-01-31');

    expect(state.startDate()).toBe('2025-01-01');
    expect(state.endDate()).toBe('2025-01-31');
    expect(state.offset()).toBe(0);
  });

  it('setResponse hydrates totals + idle status', () => {
    const state = signalState(initialTransactionsState);
    const methods = transactionsMethods(state);

    methods.setResponse(SAMPLE_RESPONSE);

    expect(state.bankName()).toBe('Chase');
    expect(state.currency()).toBe('USD');
    expect(state.totalCount()).toBe(120);
    expect(state.status()).toBe('idle');
  });

  it('nextPage / previousPage move offset by PAGE_SIZE', () => {
    const state = signalState(initialTransactionsState);
    const methods = transactionsMethods(state);

    methods.nextPage();
    expect(state.offset()).toBe(PAGE_SIZE);

    methods.nextPage();
    expect(state.offset()).toBe(PAGE_SIZE * 2);

    methods.previousPage();
    expect(state.offset()).toBe(PAGE_SIZE);
  });

  it('previousPage clamps at 0', () => {
    const state = signalState(initialTransactionsState);
    const methods = transactionsMethods(state);

    methods.previousPage();

    expect(state.offset()).toBe(0);
  });

  it('setError flags error status', () => {
    const state = signalState(initialTransactionsState);
    const methods = transactionsMethods(state);

    methods.setError('BOOM');

    expect(state.status()).toBe('error');
    expect(state.errorCode()).toBe('BOOM');
  });
});
