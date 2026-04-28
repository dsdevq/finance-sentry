import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {HoldingsStore} from '../../../holdings/store/holdings.store';
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

function buildHoldingsStore() {
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

describe('IbkrFormComponent', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('connect() dispatches store.connect with the injected strategy and undefined payload', () => {
    const store = buildConnectStore();
    const strategy = buildStrategy();
    configure(store, buildHoldingsStore(), strategy);

    const fixture = TestBed.createComponent(IbkrFormComponent);
    fixture.componentInstance.connect();

    expect(store.connect).toHaveBeenCalledWith({strategy, payload: undefined});
  });

  it('isDuplicateError flips when error code is IBKR_DUPLICATE', () => {
    const store = buildConnectStore('IBKR_DUPLICATE');
    configure(store, buildHoldingsStore(), buildStrategy());

    const fixture = TestBed.createComponent(IbkrFormComponent);
    expect(fixture.componentInstance.isDuplicateError()).toBe(true);
  });

  it('disconnectExisting calls holdingsStore.disconnectIBKR and resets error', () => {
    const store = buildConnectStore('IBKR_DUPLICATE');
    const holdings = buildHoldingsStore();
    configure(store, holdings, buildStrategy());

    const fixture = TestBed.createComponent(IbkrFormComponent);
    fixture.componentInstance.disconnectExisting();

    expect(holdings.disconnectIBKR).toHaveBeenCalledOnce();
    expect(store.resetError).toHaveBeenCalledOnce();
  });

  it('renders the single-tenant gateway notice', () => {
    configure(buildConnectStore(), buildHoldingsStore(), buildStrategy());

    const fixture = TestBed.createComponent(IbkrFormComponent);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('gateway');
  });
});
