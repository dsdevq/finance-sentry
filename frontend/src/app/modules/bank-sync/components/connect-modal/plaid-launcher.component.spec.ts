import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {ConnectStore} from '../../store/connect/connect.store';
import {type ConnectStrategy} from '../../strategies/connect-strategy';
import {CONNECT_STRATEGY} from '../../strategies/connect-strategy.token';
import {PlaidLauncherComponent} from './plaid-launcher.component';

function buildStore() {
  return {
    isBusy: signal(false),
    isInitializing: signal(false),
    statusMessage: signal<Nullable<string>>(null),
    connect: vi.fn(),
    setModalStep: vi.fn(),
  };
}

function buildStrategy(): ConnectStrategy {
  return {
    slug: 'plaid',
    formComponent: class {} as unknown as ConnectStrategy['formComponent'],
    submit: vi.fn(),
  };
}

function configure(store: ReturnType<typeof buildStore>, strategy: ConnectStrategy): void {
  TestBed.configureTestingModule({
    providers: [
      {provide: ConnectStore, useValue: store},
      {provide: CONNECT_STRATEGY, useValue: strategy},
    ],
  });
}

describe('PlaidLauncherComponent', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('launch() dispatches connect with the injected strategy and undefined payload', () => {
    const store = buildStore();
    const strategy = buildStrategy();
    configure(store, strategy);

    const fixture = TestBed.createComponent(PlaidLauncherComponent);
    fixture.componentInstance.launch();

    expect(store.connect).toHaveBeenCalledWith({strategy, payload: undefined});
  });

  it('back() returns to the bank-picker step', () => {
    const store = buildStore();
    configure(store, buildStrategy());

    const fixture = TestBed.createComponent(PlaidLauncherComponent);
    fixture.componentInstance.back();

    expect(store.setModalStep).toHaveBeenCalledWith('bank-picker');
  });
});
