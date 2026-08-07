import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {pipe, switchMap, tap} from 'rxjs';

import {StoreErrorUtils} from '../../../shared/utils/store-error.utils';
import {type Position} from '../models/position/position.model';
import {PositionsService} from '../services/positions.service';

interface StoreMethods {
  setPositionsLoading(): void;
  setPositions(positions: Position[]): void;
  setPositionsError(errorCode: Nullable<string>): void;
}

export function holdingsEffects(store: StoreMethods) {
  const positionsService = inject(PositionsService);

  const loadPositions = rxMethod<void>(
    pipe(
      tap(() => store.setPositionsLoading()),
      switchMap(() =>
        positionsService.getPositions().pipe(
          tap(positions => store.setPositions(positions)),
          StoreErrorUtils.catchAndSetError({
            setError: (code: Nullable<string>) => store.setPositionsError(code),
          })
        )
      )
    )
  );

  return {loadPositions};
}

export function holdingsHooks(store: ReturnType<typeof holdingsEffects>) {
  return {
    onInit: () => {
      store.loadPositions();
    },
  };
}
