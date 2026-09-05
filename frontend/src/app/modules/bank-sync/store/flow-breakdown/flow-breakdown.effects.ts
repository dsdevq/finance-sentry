import {inject} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {map, type Observable, pipe, switchMap, tap} from 'rxjs';

import {StoreErrorUtils} from '../../../../shared/utils/store-error.utils';
import {type FlowBreakdown} from '../../models/flow-breakdown/flow-breakdown.model';
import {BankSyncService} from '../../services/bank-sync.service';
import {MonthKeyUtils} from '../../utils/month-key.utils';

const MONTH_PARAM_PATTERN = /^\d{4}-\d{2}$/;

interface EffectsStore {
  setLoading: (month: string) => void;
  setBreakdown: (breakdown: FlowBreakdown) => void;
  setError: (errorCode: Nullable<string>) => void;
}

export function flowBreakdownEffects(store: EffectsStore) {
  const bankSyncService = inject(BankSyncService);

  return {
    load: rxMethod<string>(
      pipe(
        tap(month => store.setLoading(month)),
        switchMap(month =>
          bankSyncService.getFlowBreakdown(month).pipe(
            tap(breakdown => store.setBreakdown(breakdown)),
            StoreErrorUtils.catchAndSetError(store)
          )
        )
      )
    ),
  };
}

interface HookStore {
  load: (month$: Observable<string>) => void;
}

/**
 * Drives the page off the `month` query param; a missing/invalid param means "now".
 * rxMethod ties the subscription to the store's injector, so it tears down with the page.
 */
export function flowBreakdownHooks(store: HookStore): void {
  const route = inject(ActivatedRoute);
  store.load(
    route.queryParamMap.pipe(
      map(params => {
        const month = params.get('month');
        return month !== null && MONTH_PARAM_PATTERN.test(month)
          ? month
          : MonthKeyUtils.currentUtc();
      })
    )
  );
}
