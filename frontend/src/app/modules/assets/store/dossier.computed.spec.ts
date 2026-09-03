import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {ERROR_MESSAGES} from '@lifekit-hq/core';
import {beforeEach, describe, expect, it} from 'vitest';

import {ERROR_MESSAGES_REGISTRY} from '../../../core/errors/error-messages.registry';
import {type AssetDossierDto, type AssetLedgerReadDto} from '../models/dossier/dossier.model';
import {dossierComputed} from './dossier.computed';
import {type DossierState} from './dossier.state';

function buildSignals(overrides: Partial<DossierState> = {}) {
  return {
    dossier: signal<Nullable<AssetDossierDto>>(overrides.dossier ?? null),
    dossierStatus: signal<DossierState['dossierStatus']>(overrides.dossierStatus ?? 'idle'),
    dossierErrorCode: signal<Nullable<string>>(overrides.dossierErrorCode ?? null),
    ledgerRead: signal<Nullable<AssetLedgerReadDto>>(overrides.ledgerRead ?? null),
    ledgerReadStatus: signal<DossierState['ledgerReadStatus']>(
      overrides.ledgerReadStatus ?? 'idle'
    ),
    ledgerReadErrorCode: signal<Nullable<string>>(overrides.ledgerReadErrorCode ?? null),
  };
}

function ledgerRead(overrides: Partial<AssetLedgerReadDto> = {}): AssetLedgerReadDto {
  return {
    symbol: 'AAPL',
    narrative: 'A read.',
    generatedAt: '2026-09-03T00:00:00Z',
    isStale: false,
    cached: true,
    ...overrides,
  };
}

describe('dossierComputed', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{provide: ERROR_MESSAGES, useValue: ERROR_MESSAGES_REGISTRY}],
    });
  });

  it('isDossierLoading is true only when status is loading', () => {
    for (const status of ['loading'] as const) {
      const store = buildSignals({dossierStatus: status});
      TestBed.runInInjectionContext(() => {
        expect(dossierComputed(store).isDossierLoading()).toBe(true);
      });
    }
  });

  it('isDossierLoading is false when status is idle or error', () => {
    for (const status of ['idle', 'error'] as const) {
      const store = buildSignals({dossierStatus: status});
      TestBed.runInInjectionContext(() => {
        expect(dossierComputed(store).isDossierLoading()).toBe(false);
      });
    }
  });

  it('dossierErrorMessage is empty when status is not error', () => {
    const store = buildSignals({dossierStatus: 'loading', dossierErrorCode: 'SOME_CODE'});
    TestBed.runInInjectionContext(() => {
      expect(dossierComputed(store).dossierErrorMessage()).toBe('');
    });
  });

  it('dossierErrorMessage returns default when error code is unknown', () => {
    const store = buildSignals({dossierStatus: 'error', dossierErrorCode: 'UNKNOWN_CODE_XYZ'});
    TestBed.runInInjectionContext(() => {
      expect(dossierComputed(store).dossierErrorMessage()).toBe('Failed to load asset dossier.');
    });
  });

  it('dossierErrorMessage returns default when error code is null', () => {
    const store = buildSignals({dossierStatus: 'error', dossierErrorCode: null});
    TestBed.runInInjectionContext(() => {
      expect(dossierComputed(store).dossierErrorMessage()).toBe('Failed to load asset dossier.');
    });
  });

  it('ledgerReadNarrative is empty when nothing has been generated', () => {
    const store = buildSignals({ledgerRead: ledgerRead({narrative: null})});
    TestBed.runInInjectionContext(() => {
      expect(dossierComputed(store).ledgerReadNarrative()).toBe('');
      expect(dossierComputed(store).isLedgerReadStale()).toBe(false);
    });
  });

  it('ledgerReadNarrative and stale flag surface the cached read', () => {
    const store = buildSignals({
      ledgerRead: ledgerRead({narrative: 'AAPL is fine.', isStale: true}),
    });
    TestBed.runInInjectionContext(() => {
      expect(dossierComputed(store).ledgerReadNarrative()).toBe('AAPL is fine.');
      expect(dossierComputed(store).isLedgerReadStale()).toBe(true);
    });
  });

  it('ledgerReadErrorMessage resolves the backend error code', () => {
    const store = buildSignals({
      ledgerReadStatus: 'error',
      ledgerReadErrorCode: 'LEDGER_READ_UNAVAILABLE',
    });
    TestBed.runInInjectionContext(() => {
      expect(dossierComputed(store).ledgerReadErrorMessage()).toBe(
        'Ledger could not produce a read right now. Try again shortly.'
      );
    });
  });

  it('ledgerReadErrorMessage falls back to a default for an unknown code', () => {
    const store = buildSignals({ledgerReadStatus: 'error', ledgerReadErrorCode: 'NOPE_XYZ'});
    TestBed.runInInjectionContext(() => {
      expect(dossierComputed(store).ledgerReadErrorMessage()).toBe("Failed to load Ledger's read.");
    });
  });

  it('ledgerReadErrorMessage is empty when the status is not error', () => {
    const store = buildSignals({ledgerReadStatus: 'loading', ledgerReadErrorCode: 'X'});
    TestBed.runInInjectionContext(() => {
      expect(dossierComputed(store).ledgerReadErrorMessage()).toBe('');
      expect(dossierComputed(store).isLedgerReadLoading()).toBe(true);
    });
  });
});
