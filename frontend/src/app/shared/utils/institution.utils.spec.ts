import {describe, expect, it} from 'vitest';

import {type AccountBalanceItem} from '../models/wealth/wealth.model';
import {InstitutionUtils} from './institution.utils';

function acct(overrides: Partial<AccountBalanceItem>): AccountBalanceItem {
  return {
    accountId: 'a',
    bankName: 'Monobank',
    accountType: 'card',
    accountNumberLast4: '0000',
    provider: 'monobank',
    category: 'banking',
    currency: 'UAH',
    currentBalance: 0,
    balanceInBaseCurrency: 0,
    syncStatus: 'synced',
    lastSyncTimestamp: null,
    ...overrides,
  } as AccountBalanceItem;
}

describe('InstitutionUtils.groupByInstitution', () => {
  it('collapses monobank cards under a single institution row', () => {
    const groups = InstitutionUtils.groupByInstitution([
      acct({accountId: '1', accountNumberLast4: '1111', balanceInBaseCurrency: 100}),
      acct({accountId: '2', accountNumberLast4: '2222', balanceInBaseCurrency: 250}),
      acct({accountId: '3', accountNumberLast4: '3333', balanceInBaseCurrency: 50}),
    ]);

    expect(groups).toHaveLength(1);
    expect(groups[0].name).toBe('Monobank');
    expect(groups[0].accounts).toHaveLength(3);
    expect(groups[0].totalInBaseCurrency).toBe(400);
  });

  it('keeps different bankName rows in separate institutions (Revolut vs AIB)', () => {
    const groups = InstitutionUtils.groupByInstitution([
      acct({provider: 'truelayer', bankName: 'Revolut', balanceInBaseCurrency: 1000}),
      acct({provider: 'truelayer', bankName: 'AIB', balanceInBaseCurrency: 500}),
    ]);

    expect(groups).toHaveLength(2);
    expect(groups.map(g => g.name).sort()).toEqual(['AIB', 'Revolut']);
  });

  it('sorts institutions by totalInBaseCurrency desc', () => {
    const groups = InstitutionUtils.groupByInstitution([
      acct({provider: 'truelayer', bankName: 'AIB', balanceInBaseCurrency: 500}),
      acct({provider: 'monobank', bankName: 'Monobank', balanceInBaseCurrency: 2000}),
      acct({provider: 'truelayer', bankName: 'Revolut', balanceInBaseCurrency: 1000}),
    ]);

    expect(groups.map(g => g.name)).toEqual(['Monobank', 'Revolut', 'AIB']);
  });

  it('groups Binance assets and IBKR positions under one row each', () => {
    const groups = InstitutionUtils.groupByInstitution([
      acct({
        provider: 'binance',
        bankName: 'Binance',
        category: 'crypto',
        balanceInBaseCurrency: 1000,
      }),
      acct({
        provider: 'binance',
        bankName: 'Binance',
        category: 'crypto',
        balanceInBaseCurrency: 500,
      }),
      acct({
        provider: 'ibkr',
        bankName: 'IBKR',
        category: 'brokerage',
        balanceInBaseCurrency: 10000,
      }),
    ]);

    expect(groups).toHaveLength(2);
    const binance = groups.find(g => g.name === 'Binance')!;
    const ibkr = groups.find(g => g.name === 'IBKR')!;
    expect(binance.accounts).toHaveLength(2);
    expect(binance.totalInBaseCurrency).toBe(1500);
    expect(ibkr.accounts).toHaveLength(1);
    expect(ibkr.totalInBaseCurrency).toBe(10000);
  });

  it('worstSyncStatus surfaces the most degraded state across accounts', () => {
    const groups = InstitutionUtils.groupByInstitution([
      acct({accountId: '1', syncStatus: 'synced'}),
      acct({accountId: '2', syncStatus: 'failed'}),
      acct({accountId: '3', syncStatus: 'synced'}),
    ]);

    expect(groups[0].worstSyncStatus).toBe('failed');
  });

  it('latestSyncTimestamp picks the max across accounts', () => {
    const groups = InstitutionUtils.groupByInstitution([
      acct({accountId: '1', lastSyncTimestamp: '2026-06-01T00:00:00Z'}),
      acct({accountId: '2', lastSyncTimestamp: '2026-07-01T00:00:00Z'}),
      acct({accountId: '3', lastSyncTimestamp: null}),
    ]);

    expect(groups[0].latestSyncTimestamp).toBe('2026-07-01T00:00:00Z');
  });

  it('returns an empty array when input is empty', () => {
    expect(InstitutionUtils.groupByInstitution([])).toEqual([]);
  });
});
