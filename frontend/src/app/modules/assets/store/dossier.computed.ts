import {computed, inject, type Signal} from '@angular/core';
import {ErrorMessageService} from '@lifekit-hq/core';

import {type AssetDossierDto, type AssetLedgerReadDto} from '../models/dossier/dossier.model';
import {type DossierState} from './dossier.state';

interface StateSignals {
  dossier: Signal<Nullable<AssetDossierDto>>;
  dossierStatus: Signal<DossierState['dossierStatus']>;
  dossierErrorCode: Signal<Nullable<string>>;
  ledgerRead: Signal<Nullable<AssetLedgerReadDto>>;
  ledgerReadStatus: Signal<DossierState['ledgerReadStatus']>;
  ledgerReadErrorCode: Signal<Nullable<string>>;
}

const DEFAULT_DOSSIER_ERROR = 'Failed to load asset dossier.';
const DEFAULT_LEDGER_READ_ERROR = "Failed to load Ledger's read.";

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
    isLedgerReadLoading: computed(() => store.ledgerReadStatus() === 'loading'),
    ledgerReadNarrative: computed(() => store.ledgerRead()?.narrative ?? ''),
    isLedgerReadStale: computed(() => store.ledgerRead()?.isStale === true),
    ledgerReadErrorMessage: computed(() => {
      if (store.ledgerReadStatus() !== 'error') {
        return '';
      }
      return errorMessages.resolve(store.ledgerReadErrorCode()) ?? DEFAULT_LEDGER_READ_ERROR;
    }),
  };
}
