import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {beforeEach, describe, expect, it, vi} from 'vitest';

import {type Provider} from '../../../../shared/models/provider/provider.model';
import {ConnectStore} from '../../store/connect/connect.store';
import {BankPickerComponent} from './bank-picker.component';

function buildStore(connected: ReadonlySet<Provider> = new Set()) {
  return {
    connectedProviders: signal(connected),
    selectBankProvider: vi.fn(),
    setModalStep: vi.fn(),
  };
}

function configure(store: ReturnType<typeof buildStore>): void {
  TestBed.configureTestingModule({providers: [{provide: ConnectStore, useValue: store}]});
}

describe('BankPickerComponent', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('exposes only bank-typed providers', () => {
    configure(buildStore());
    const fixture = TestBed.createComponent(BankPickerComponent);
    const slugs = fixture.componentInstance.providers.map(p => p.slug).sort();
    expect(slugs).toEqual(['monobank', 'plaid']);
    for (const p of fixture.componentInstance.providers) {
      expect(p.institutionType).toBe('bank');
    }
  });

  it('select() forwards to store.selectBankProvider', () => {
    const store = buildStore();
    configure(store);
    const fixture = TestBed.createComponent(BankPickerComponent);
    fixture.componentInstance.select('plaid');
    expect(store.selectBankProvider).toHaveBeenCalledWith('plaid');
  });

  it('back() returns to the type-picker step', () => {
    const store = buildStore();
    configure(store);
    const fixture = TestBed.createComponent(BankPickerComponent);
    fixture.componentInstance.back();
    expect(store.setModalStep).toHaveBeenCalledWith('type-picker');
  });

  it('connected() reflects the store signal', () => {
    const set = new Set<Provider>(['plaid']);
    const store = buildStore(set);
    configure(store);
    const fixture = TestBed.createComponent(BankPickerComponent);
    expect(fixture.componentInstance.connected()).toBe(set);
  });
});
