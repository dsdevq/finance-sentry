import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {catchError, EMPTY, pipe, switchMap, tap} from 'rxjs';

import {type WealthSummaryResponse} from '../../../../shared/models/wealth/wealth.model';
import {ErrorUtils} from '../../../../shared/utils/error.utils';
import {WealthService} from '../../services/wealth.service';

interface EffectsStore {
  setLoading: () => void;
  setSummary: (summary: WealthSummaryResponse) => void;
  setError: (errorCode: Nullable<string>) => void;
}

export function accountsEffects(store: EffectsStore) {
  const wealthService = inject(WealthService);

  return {
    load: rxMethod<void>(
      pipe(
        tap(() => store.setLoading()),
        switchMap(() =>
          wealthService.getSummary().pipe(
            tap(summary => store.setSummary(summary)),
            catchError((err: unknown) => {
              store.setError(ErrorUtils.extractCode(err));
              return EMPTY;
            })
          )
        )
      )
    ),
  };
}

interface HookStore {
  load: () => void;
}

export function accountsHooks(store: HookStore): void {
  store.load();
}
