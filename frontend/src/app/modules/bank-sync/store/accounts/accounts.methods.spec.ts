import {signalState} from '@ngrx/signals';
import {describe, expect, it} from 'vitest';

import {type BankAccount} from '../../models/bank-account.model';
import {accountsMethods} from './accounts.methods';
import {initialAccountsState} from './accounts.state';

const ACCOUNT_A = {accountId: 'a1', bankName: 'A'} as unknown as BankAccount;
const ACCOUNT_B = {accountId: 'b2', bankName: 'B'} as unknown as BankAccount;

describe('accountsMethods', () => {
  it('setLoading clears error and flips status', () => {
    const state = signalState(initialAccountsState);
    const methods = accountsMethods(state);
    methods.setError('X');

    methods.setLoading();

    expect(state.status()).toBe('loading');
    expect(state.errorCode()).toBeNull();
  });

  it('setAccounts stores the list and returns to idle', () => {
    const state = signalState(initialAccountsState);
    const methods = accountsMethods(state);

    methods.setAccounts([ACCOUNT_A, ACCOUNT_B]);

    expect(state.accounts()).toEqual([ACCOUNT_A, ACCOUNT_B]);
    expect(state.status()).toBe('idle');
  });

  it('setError flags error status with code', () => {
    const state = signalState(initialAccountsState);
    const methods = accountsMethods(state);

    methods.setError('BOOM');

    expect(state.status()).toBe('error');
    expect(state.errorCode()).toBe('BOOM');
  });

  it('removeAccount removes the account matching the id', () => {
    const state = signalState(initialAccountsState);
    const methods = accountsMethods(state);
    methods.setAccounts([ACCOUNT_A, ACCOUNT_B]);

    methods.removeAccount('a1');

    expect(state.accounts()).toEqual([ACCOUNT_B]);
  });

  it('removeAccount is a no-op when id is unknown', () => {
    const state = signalState(initialAccountsState);
    const methods = accountsMethods(state);
    methods.setAccounts([ACCOUNT_A]);

    methods.removeAccount('unknown');

    expect(state.accounts()).toEqual([ACCOUNT_A]);
  });
});
