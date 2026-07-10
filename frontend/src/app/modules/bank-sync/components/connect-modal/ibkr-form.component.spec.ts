import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {AccountsStore} from '../../store/accounts/accounts.store';
import {ConnectStore} from '../../store/connect/connect.store';
import {type ConnectStrategy} from '../../strategies/connect-strategy';
import {CONNECT_STRATEGY} from '../../strategies/connect-strategy.token';
import {IbkrFormComponent} from './ibkr-form.component';

function buildConnectStore(errorCode: Nullable<string> = null) {
  return {
    errorCode: signal<Nullable<string>>(errorCode),
    errorMessage: signal('IBKR account already connected'),
    isBusy: signal(false),
    connect: vi.fn(),
    setModalStep: vi.fn(),
    resetError: vi.fn(),
  };
}

function buildAccountsStore() {
  return {disconnectBinance: vi.fn(), disconnectIBKR: vi.fn()};
}

function buildStrategy(): ConnectStrategy {
  return {
    slug: 'ibkr',
    formComponent: class {} as unknown as ConnectStrategy['formComponent'],
    submit: vi.fn(),
  };
}

function configure(
  store: ReturnType<typeof buildConnectStore>,
  accounts: ReturnType<typeof buildAccountsStore>,
  strategy: ConnectStrategy
): void {
  TestBed.configureTestingModule({
    providers: [
      {provide: ConnectStore, useValue: store},
      {provide: AccountsStore, useValue: accounts},
      {provide: CONNECT_STRATEGY, useValue: strategy},
    ],
  });
}

describe('IbkrFormComponent', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('submit() dispatches store.connect with the trimmed credentials', () => {
    const store = buildConnectStore();
    const strategy = buildStrategy();
    configure(store, buildAccountsStore(), strategy);

    const fixture = TestBed.createComponent(IbkrFormComponent);
    const cmp = fixture.componentInstance;
    cmp.form.setValue({username: '  ibkr-user  ', password: 'ibkr-pass'});

    cmp.submit();

    expect(store.connect).toHaveBeenCalledWith({
      strategy,
      payload: {username: 'ibkr-user', password: 'ibkr-pass'},
    });
  });

  it('submit() blocks and marks touched when the form is invalid', () => {
    const store = buildConnectStore();
    configure(store, buildAccountsStore(), buildStrategy());

    const fixture = TestBed.createComponent(IbkrFormComponent);
    fixture.componentInstance.submit();

    expect(store.connect).not.toHaveBeenCalled();
    expect(fixture.componentInstance.form.controls.username.touched).toBe(true);
  });

  it('isDuplicateError flips when error code is IBKR_DUPLICATE', () => {
    const store = buildConnectStore('IBKR_DUPLICATE');
    configure(store, buildAccountsStore(), buildStrategy());

    const fixture = TestBed.createComponent(IbkrFormComponent);
    expect(fixture.componentInstance.isDuplicateError()).toBe(true);
  });

  it('disconnectExisting calls accountsStore.disconnectIBKR, resets error, and clears the form', () => {
    const store = buildConnectStore('IBKR_DUPLICATE');
    const accounts = buildAccountsStore();
    configure(store, accounts, buildStrategy());

    const fixture = TestBed.createComponent(IbkrFormComponent);
    const cmp = fixture.componentInstance;
    cmp.form.setValue({username: 'x', password: 'y'});

    cmp.disconnectExisting();

    expect(accounts.disconnectIBKR).toHaveBeenCalledOnce();
    expect(store.resetError).toHaveBeenCalledOnce();
    expect(cmp.form.controls.username.value).toBe('');
    expect(cmp.form.controls.password.value).toBe('');
  });
});
