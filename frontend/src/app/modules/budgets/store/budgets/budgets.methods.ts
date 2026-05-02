import {patchState, type WritableStateSource} from '@ngrx/signals';

import {type Budget} from '../../models/budget/budget.model';
import {type BudgetsState} from './budgets.state';

export function budgetsMethods(store: WritableStateSource<BudgetsState>) {
  return {
    setData(budgets: Budget[]): void {
      patchState(store, {budgets, status: 'idle'});
    },
    setEditing(category: Nullable<string>): void {
      patchState(store, {editingCategory: category});
    },
    updateLimit(category: string, limit: number): void {
      patchState(store, (s: BudgetsState) => ({
        budgets: s.budgets.map(b => (b.category === category ? {...b, limit} : b)),
        editingCategory: null,
      }));
    },
  };
}
