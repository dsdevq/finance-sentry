import {signalState} from '@ngrx/signals';
import {describe, expect, it} from 'vitest';

import {type AssetDossierDto} from '../models/dossier/dossier.model';
import {dossierMethods} from './dossier.methods';
import {initialDossierState} from './dossier.state';

function mkDossier(overrides: Partial<AssetDossierDto> = {}): AssetDossierDto {
  return {
    symbol: 'AAPL',
    position: null,
    thesis: null,
    valuation: null,
    analysts: null,
    recentNews: [],
    nextEarnings: null,
    radarSignals: [],
    generatedAt: '2026-09-01T10:00:00Z',
    ...overrides,
  };
}

describe('dossierMethods', () => {
  it('setDossierLoading transitions to loading and clears error', () => {
    const state = signalState({...initialDossierState, dossierStatus: 'error' as const, dossierErrorCode: 'SOME_ERROR'});
    const methods = dossierMethods(state);

    methods.setDossierLoading();

    expect(state.dossierStatus()).toBe('loading');
    expect(state.dossierErrorCode()).toBeNull();
  });

  it('setDossier stores the dossier and resets status to idle', () => {
    const state = signalState({...initialDossierState, dossierStatus: 'loading' as const});
    const methods = dossierMethods(state);
    const dossier = mkDossier({symbol: 'MSFT'});

    methods.setDossier(dossier);

    expect(state.dossier()).toEqual(dossier);
    expect(state.dossierStatus()).toBe('idle');
    expect(state.dossierErrorCode()).toBeNull();
  });

  it('setDossierError stores the error code and sets error status', () => {
    const state = signalState({...initialDossierState, dossierStatus: 'loading' as const});
    const methods = dossierMethods(state);

    methods.setDossierError('DOSSIER_NOT_FOUND');

    expect(state.dossierStatus()).toBe('error');
    expect(state.dossierErrorCode()).toBe('DOSSIER_NOT_FOUND');
  });

  it('setDossierError accepts null error code', () => {
    const state = signalState(initialDossierState);
    const methods = dossierMethods(state);

    methods.setDossierError(null);

    expect(state.dossierStatus()).toBe('error');
    expect(state.dossierErrorCode()).toBeNull();
  });

  it('setDossierLoading clears any previously loaded dossier status', () => {
    const state = signalState({...initialDossierState, dossierStatus: 'idle' as const});
    const methods = dossierMethods(state);
    methods.setDossier(mkDossier());

    methods.setDossierLoading();

    expect(state.dossierStatus()).toBe('loading');
    expect(state.dossier()).not.toBeNull();
  });
});
