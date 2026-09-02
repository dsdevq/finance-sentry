import {inject} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {pipe, switchMap, tap} from 'rxjs';

import {ASSET_DOSSIER_SYMBOL_PARAM} from '../../../shared/enums/app-route/app-route.enum';
import {StoreErrorUtils} from '../../../shared/utils/store-error.utils';
import {DossierService} from '../services/dossier.service';

interface StoreMethods {
  setDossierLoading(): void;
  setDossier(dossier: import('../models/dossier/dossier.model').AssetDossierDto): void;
  setDossierError(errorCode: Nullable<string>): void;
}

export function dossierEffects(store: StoreMethods) {
  const dossierService = inject(DossierService);

  const loadDossier = rxMethod<string>(
    pipe(
      tap(() => store.setDossierLoading()),
      switchMap(symbol =>
        dossierService.getDossier(symbol).pipe(
          tap(dossier => store.setDossier(dossier)),
          StoreErrorUtils.catchAndSetError({
            setError: (code: Nullable<string>) => store.setDossierError(code),
          })
        )
      )
    )
  );

  return {loadDossier};
}

export function dossierHooks(store: ReturnType<typeof dossierEffects>) {
  const route = inject(ActivatedRoute);

  return {
    onInit: () => {
      const symbol = route.snapshot.paramMap.get(ASSET_DOSSIER_SYMBOL_PARAM) ?? '';
      if (symbol) {
        store.loadDossier(symbol);
      }
    },
  };
}
