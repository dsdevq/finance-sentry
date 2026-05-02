import {type Budget} from '../../models/budget/budget.model';

export interface BudgetsState {
  budgets: Budget[];
  editingCategory: Nullable<string>;
  status: AsyncStatus;
}

export const initialBudgetsState: BudgetsState = {
  budgets: [],
  editingCategory: null,
  status: 'idle',
};
