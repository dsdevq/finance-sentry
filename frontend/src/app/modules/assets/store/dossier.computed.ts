import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@lifekit-hq/core';

import {type AssetDossierDto} from '../models/dossier/dossier.model';
import {type DossierState} from './dossier.state';

interface StateSignals {
  dossier: Signal<Nullable<AssetDossierDto>>;
  dossierStatus: Signal<DossierState['dossierStatus']>;
  dossierErrorCode: Signal<Nullable<string>>;
}

const DEFAULT_DOSSIER_ERROR = 'Failed to load asset dossier.';

export function dossierComputed(store: StateSignals) {
  const errorMessages = inject(ErrorMessageService);

  return {
    isDossierLoading: computed(() => store.dossierStatus() === 'loading'),
    dossierErrorMessage: computed(() => {
      if (store.dossierStatus() !== 'error') {
        return '';
      }
      return errorMessages.resolve(store.dossierErrorCode()) ?? DEFAULT_DOSSIER_ERROR;
    }),
  };
}
