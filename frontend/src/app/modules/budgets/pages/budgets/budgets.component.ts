import {ChangeDetectionStrategy, Component, inject, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {AlertComponent, CardComponent, TagComponent} from '@dsdevq-common/ui';

import {AppCurrencyPipe} from '../../../../core/pipes/app-currency.pipe';
import {AppDecimalPipe} from '../../../../core/pipes/app-decimal.pipe';
import {
  BUDGETS_MONTHS_IN_YEAR,
  CATEGORY_COLOR_FALLBACK,
  CATEGORY_COLOR_MAP,
  VALID_BUDGET_CATEGORIES,
} from '../../constants/budget/budget.constants';
import {BudgetsStore} from '../../store/budgets/budgets.store';

const PCT_MAX = 100;
const PCT_WARNING_THRESHOLD = 80;

@Component({
  selector: 'fns-budgets',
  imports: [
    AlertComponent,
    AppCurrencyPipe,
    AppDecimalPipe,
    TagComponent,
    CardComponent,
    FormsModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [BudgetsStore],
  templateUrl: './budgets.component.html',
})
export class BudgetsComponent {
  public readonly store = inject(BudgetsStore);

  public readonly editValue = signal('');
  public readonly newCategory = signal('');
  public readonly newLimit = signal('');

  public readonly VALID_CATEGORIES = VALID_BUDGET_CATEGORIES;

  public categoryColor(category: string): string {
    return CATEGORY_COLOR_MAP.get(category) ?? CATEGORY_COLOR_FALLBACK;
  }

  public barPct(spent: number, limit: number): number {
    return Math.min((spent / limit) * PCT_MAX, PCT_MAX);
  }

  public barColor(spent: number, limit: number, category: string): string {
    const pct = (spent / limit) * PCT_MAX;
    if (spent > limit) {
      return 'var(--color-status-error)';
    }
    if (pct >= PCT_WARNING_THRESHOLD) {
      return 'var(--color-status-warning)';
    }
    return this.categoryColor(category);
  }

  public startEdit(id: string, monthlyLimit: number): void {
    this.editValue.set(String(monthlyLimit));
    this.store.setEditing(id);
  }

  public saveEdit(id: string): void {
    const val = parseFloat(this.editValue());
    if (!isNaN(val) && val > 0) {
      this.store.update({id, monthlyLimit: val});
    }
    this.store.setEditing(null);
  }

  public cancelEdit(): void {
    this.store.setEditing(null);
  }

  public createBudget(): void {
    const cat = this.newCategory();
    const lim = parseFloat(this.newLimit());
    if (!cat || isNaN(lim) || lim <= 0) {
      return;
    }
    this.store.create({category: cat, monthlyLimit: lim});
    this.newCategory.set('');
    this.newLimit.set('');
  }

  public deleteBudget(id: string): void {
    this.store.remove(id);
  }

  public previousMonth(): void {
    const year = this.store.selectedYear();
    const month = this.store.selectedMonth();
    if (month === 1) {
      this.store.navigateToPeriod({year: year - 1, month: BUDGETS_MONTHS_IN_YEAR});
    } else {
      this.store.navigateToPeriod({year, month: month - 1});
    }
  }

  public nextMonth(): void {
    const year = this.store.selectedYear();
    const month = this.store.selectedMonth();
    const now = new Date();
    const isCurrentMonth = year === now.getFullYear() && month === now.getMonth() + 1;
    if (isCurrentMonth) {
      return;
    }
    if (month === BUDGETS_MONTHS_IN_YEAR) {
      this.store.navigateToPeriod({year: year + 1, month: 1});
    } else {
      this.store.navigateToPeriod({year, month: month + 1});
    }
  }
}
