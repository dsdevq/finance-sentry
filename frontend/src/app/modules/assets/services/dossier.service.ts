import {Injectable} from '@angular/core';
import {ApiService} from '@lifekit-hq/core';
import {type Observable} from 'rxjs';

import {type AssetDossierDto} from '../models/dossier/dossier.model';

@Injectable({providedIn: 'root'})
export class DossierService extends ApiService {
  constructor() {
    super('');
  }

  public getDossier(symbol: string): Observable<AssetDossierDto> {
    return this.get<AssetDossierDto>(`research/assets/${encodeURIComponent(symbol)}/dossier`);
  }
}
