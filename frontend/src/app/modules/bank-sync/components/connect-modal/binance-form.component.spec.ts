import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {HoldingsStore} from '../../../holdings/store/holdings.store';
import {ConnectStore} from '../../store/connect/connect.store';
import {type ConnectStrategy} from '../../strategies/connect-strategy';
import {CONNECT_STRATEGY} from '../../strategies/connect-strategy.token';
import {BinanceFormComponent} from './binance-form.component';

function buildConnectStore(errorCode: Nullable<string> = null) {
  return {
    errorCode: signal<Nullable<string>>(errorCode),
    errorMessage: signal('Account already connected'),
    isBusy: signal(false),
    connect: vi.fn(),
    setModalStep: vi.fn(),
    resetError: vi.fn(),
  };
}

function buildHoldingsStore() {
  return {disconnectBinance: vi.fn(), disconnectIBKR: vi.fn()};
}

function buildStrategy(): ConnectStrategy {
  return {
    slug: 'binance',
    formComponent: class {} as unknown as ConnectStrategy['formComponent'],
    submit: vi.fn(),
  };
}

function configure(
  store: ReturnType<typeof buildConnectStore>,
  holdings: ReturnType<typeof buildHoldingsStore>,
  strategy: ConnectStrategy
): void {
  TestBed.configureTestingModule({
    providers: [
      {provide: ConnectStore, useValue: store},
      {provide: HoldingsStore, useValue: holdings},
      {provide: CONNECT_STRATEGY, useValue: strategy},
    ],
  });
}

describe('BinanceFormComponent', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('does not dispatch when either field is empty', () => {
    const store = buildConnectStore();
    const strategy = buildStrategy();
    configure(store, buildHoldingsStore(), strategy);

    const fixture = TestBed.createComponent(BinanceFormComponent);
    fixture.componentInstance.form.controls.apiKey.setValue('keyOnly');
    fixture.componentInstance.submit();

    expect(store.connect).not.toHaveBeenCalled();
  });

  it('dispatches connect with trimmed key + secret when both are filled', () => {
    const store = buildConnectStore();
    const strategy = buildStrategy();
    configure(store, buildHoldingsStore(), strategy);

    const fixture = TestBed.createComponent(BinanceFormComponent);
    fixture.componentInstance.form.setValue({
      apiKey: '  abc123  ',
      apiSecret: '  s3cret  ',
    });
    fixture.componentInstance.submit();

    expect(store.connect).toHaveBeenCalledWith({
      strategy,
      payload: {apiKey: 'abc123', apiSecret: 's3cret'},
    });
  });

  it('isDuplicateError flips when error code is BINANCE_DUPLICATE', () => {
    const store = buildConnectStore('BINANCE_DUPLICATE');
    configure(store, buildHoldingsStore(), buildStrategy());

    const fixture = TestBed.createComponent(BinanceFormComponent);
    expect(fixture.componentInstance.isDuplicateError()).toBe(true);
  });

  it('disconnectExisting calls holdingsStore.disconnectBinance and resets error/form', () => {
    const store = buildConnectStore('BINANCE_DUPLICATE');
    const holdings = buildHoldingsStore();
    configure(store, holdings, buildStrategy());

    const fixture = TestBed.createComponent(BinanceFormComponent);
    fixture.componentInstance.form.setValue({apiKey: 'k', apiSecret: 's'});
    fixture.componentInstance.disconnectExisting();

    expect(holdings.disconnectBinance).toHaveBeenCalledOnce();
    expect(store.resetError).toHaveBeenCalledOnce();
    expect(fixture.componentInstance.form.getRawValue()).toEqual({apiKey: '', apiSecret: ''});
  });
});
