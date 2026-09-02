import {type AssetDossierDto} from '../models/dossier/dossier.model';

export interface DossierState {
  dossier: Nullable<AssetDossierDto>;
  dossierStatus: AsyncStatus;
  dossierErrorCode: Nullable<string>;
}

export const initialDossierState: DossierState = {
  dossier: null,
  dossierStatus: 'idle',
  dossierErrorCode: null,
};
