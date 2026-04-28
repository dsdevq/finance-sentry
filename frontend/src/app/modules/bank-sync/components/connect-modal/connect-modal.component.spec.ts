import {signal, type Type} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it} from 'vitest';

import {type ModalStep} from '../../models/connect/connect.model';
import {ConnectStore} from '../../store/connect/connect.store';
import {type ConnectStrategy} from '../../strategies/connect-strategy';
import {CONNECT_STRATEGIES, ConnectStrategyRegistry} from '../../strategies/connect-strategy.token';
import {ConnectModalComponent} from './connect-modal.component';

class PlaidFormStub {}
class MonobankFormStub {}
class BinanceFormStub {}
class IbkrFormStub {}

const STRATEGIES: readonly ConnectStrategy[] = [
  {slug: 'plaid', formComponent: PlaidFormStub as Type<unknown>, submit: () => null as never},
  {
    slug: 'monobank',
    formComponent: MonobankFormStub as Type<unknown>,
    submit: () => null as never,
  },
  {slug: 'binance', formComponent: BinanceFormStub as Type<unknown>, submit: () => null as never},
  {slug: 'ibkr', formComponent: IbkrFormStub as Type<unknown>, submit: () => null as never},
];

function buildStore(modalStep: ModalStep) {
  return {modalStep: signal<ModalStep>(modalStep)};
}

function configure(store: ReturnType<typeof buildStore>): void {
  TestBed.configureTestingModule({
    providers: [
      {provide: ConnectStore, useValue: store},
      {provide: CONNECT_STRATEGIES, useValue: STRATEGIES},
      ConnectStrategyRegistry,
    ],
  });
}

describe('ConnectModalComponent', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it.each([
    ['plaid-launcher', PlaidFormStub],
    ['monobank-form', MonobankFormStub],
    ['binance-form', BinanceFormStub],
    ['ibkr-form', IbkrFormStub],
  ] as const)('resolves the strategy form component for %s', (step, expected) => {
    configure(buildStore(step));
    const fixture = TestBed.createComponent(ConnectModalComponent);
    expect(fixture.componentInstance.formComponent()).toBe(expected);
    expect(fixture.componentInstance.formInjector()).not.toBeNull();
  });

  it('returns null formComponent for non-form steps', () => {
    configure(buildStore('type-picker'));
    const fixture = TestBed.createComponent(ConnectModalComponent);
    expect(fixture.componentInstance.formComponent()).toBeNull();
    expect(fixture.componentInstance.formInjector()).toBeNull();
  });
});
