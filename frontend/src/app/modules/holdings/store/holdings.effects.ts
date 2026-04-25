import {inject} from '@angular/core';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {pipe, switchMap, tap} from 'rxjs';

import {HoldingsService} from '../services/holdings.service';
import {type HoldingsState} from './holdings.state';

interface StoreMethods {
  setLoading(): void;
  setSummary(summary: HoldingsState['summary'] & {}): void;
  setError(errorCode: string | null): void;
}

function extractErrorCode(err: unknown): string | null {
  if (err && typeof err === 'object' && 'error' in err) {
    const body = (err as {error: unknown}).error;
    if (body && typeof body === 'object' && 'errorCode' in body) {
      return String((body as {errorCode: unknown}).errorCode);
    }
  }
  return null;
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
              error: err => store.setError(extractErrorCode(err)),
            })
          )
        )
      )
    ),
  };
}

export function holdingsHooks(store: ReturnType<typeof holdingsEffects>) {
  return {
    onInit(): void {
      store.load();
    },
  };
}
