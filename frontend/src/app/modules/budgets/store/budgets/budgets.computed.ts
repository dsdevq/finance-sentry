import {computed, type Signal} from '@angular/core';

import {type Budget} from '../../models/budget/budget.model';

const PCT_MAX = 100;

interface StateSignals {
  budgets: Signal<Budget[]>;
  editingCategory: Signal<Nullable<string>>;
}

export function budgetsComputed(store: StateSignals) {
  return {
    totalSpent: computed(() => store.budgets().reduce((s, b) => s + b.spent, 0)),
    totalBudget: computed(() => store.budgets().reduce((s, b) => s + b.limit, 0)),
    overBudgetCount: computed(() => store.budgets().filter(b => b.spent > b.limit).length),
    overallPct: computed(() => {
      const total = store.budgets().reduce((s, b) => s + b.limit, 0);
      const spent = store.budgets().reduce((s, b) => s + b.spent, 0);
      return total > 0 ? Math.min((spent / total) * PCT_MAX, PCT_MAX) : 0;
    }),
  };
}
