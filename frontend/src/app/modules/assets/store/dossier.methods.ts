import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type AssetDossierDto} from '../models/dossier/dossier.model';
import {type DossierState} from './dossier.state';

export function dossierMethods(store: WritableStateSource<DossierState>) {
  return {
    setDossierLoading(): void {
      patchState(store, {dossierStatus: 'loading', dossierErrorCode: null});
    },
    setDossier(dossier: AssetDossierDto): void {
      patchState(store, {dossier, dossierStatus: 'idle', dossierErrorCode: null});
    },
    setDossierError(errorCode: Nullable<string>): void {
      patchState(store, {dossierStatus: 'error', dossierErrorCode: errorCode});
    },
  };
}
