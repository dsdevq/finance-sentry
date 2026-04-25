import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {pipe, switchMap, tap} from 'rxjs';

import {ErrorUtils} from '../../../shared/utils/error.utils';
import {HoldingsService} from '../services/holdings.service';
import {type HoldingsState} from './holdings.state';

interface StoreMethods {
  setLoading(): void;
  setSummary(summary: HoldingsState['summary'] & {}): void;
  setError(errorCode: Nullable<string>): void;
}

export function holdingsEffects(store: StoreMethods) {
  const holdingsService = inject(HoldingsService);

  return {
    load: rxMethod<void>(
      pipe(
        tap(() => store.setLoading()),
        switchMap(() =>
          holdingsService.getSummary().pipe(
            tap({
              next: summary => store.setSummary(summary),
              error: err => store.setError(ErrorUtils.extractCode(err)),
            })
          )
        )
      )
    ),
  };
}

export function holdingsHooks(store: ReturnType<typeof holdingsEffects>) {
  return {
    onInit: () => store.load(),
  };
}
