import {describe, expect, it} from 'vitest';

import {initialDossierState} from './dossier.state';

describe('initialDossierState', () => {
  it('starts with no dossier loaded', () => {
    expect(initialDossierState.dossier).toBeNull();
  });

  it('starts in idle status', () => {
    expect(initialDossierState.dossierStatus).toBe('idle');
  });

  it('starts with no error code', () => {
    expect(initialDossierState.dossierErrorCode).toBeNull();
  });
});
