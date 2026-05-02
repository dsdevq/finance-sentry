import {rxMethod} from '@ngrx/signals/rxjs-interop';
import {of, pipe, switchMap, tap} from 'rxjs';

import {BUDGET_MOCK_DATA} from '../../constants/budget/budget.constants';
import {type Budget} from '../../models/budget/budget.model';

interface EffectsStore {
  setData: (budgets: Budget[]) => void;
}

export function budgetsEffects(store: EffectsStore) {
  return {
    load: rxMethod<void>(
      pipe(
        switchMap(() => of(BUDGET_MOCK_DATA)),
        tap(budgets => store.setData(budgets))
      )
    ),
  };
}

interface HookStore extends EffectsStore {
  load: () => void;
}

export function budgetsHooks(store: HookStore): void {
  store.load();
}
