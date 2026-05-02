import {signalStore, withComputed, withHooks, withMethods, withState} from '@ngrx/signals';

import {budgetsComputed} from './budgets.computed';
import {budgetsEffects, budgetsHooks} from './budgets.effects';
import {budgetsMethods} from './budgets.methods';
import {initialBudgetsState} from './budgets.state';

export const BudgetsStore = signalStore(
  withState(initialBudgetsState),
  withMethods(budgetsMethods),
  withComputed(budgetsComputed),
  withMethods(budgetsEffects),
  withHooks({onInit: budgetsHooks})
);
