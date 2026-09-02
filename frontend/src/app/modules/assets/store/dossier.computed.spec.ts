import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {ERROR_MESSAGES} from '@lifekit-hq/core';
import {beforeEach, describe, expect, it} from 'vitest';

import {ERROR_MESSAGES_REGISTRY} from '../../../core/errors/error-messages.registry';
import {type AssetDossierDto} from '../models/dossier/dossier.model';
import {dossierComputed} from './dossier.computed';
import {type DossierState} from './dossier.state';

function buildSignals(overrides: Partial<DossierState> = {}) {
  return {
    dossier: signal<Nullable<AssetDossierDto>>(overrides.dossier ?? null),
    dossierStatus: signal<DossierState['dossierStatus']>(overrides.dossierStatus ?? 'idle'),
    dossierErrorCode: signal<Nullable<string>>(overrides.dossierErrorCode ?? null),
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
});
