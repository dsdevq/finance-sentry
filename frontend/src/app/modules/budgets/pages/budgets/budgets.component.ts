import {ChangeDetectionStrategy, Component, inject, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {AlertComponent, BadgeComponent, CardComponent} from '@dsdevq-common/ui';

import {BudgetsStore} from '../../store/budgets/budgets.store';

const USD = new Intl.NumberFormat('en-US', {style: 'currency', currency: 'USD'});
const PCT_MAX = 100;
const PCT_WARNING_THRESHOLD = 80;

@Component({
  selector: 'fns-budgets',
  imports: [AlertComponent, BadgeComponent, CardComponent, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [BudgetsStore],
  templateUrl: './budgets.component.html',
})
export class BudgetsComponent {
  public readonly store = inject(BudgetsStore);

  public readonly editValue = signal('');

  public fmt(n: number): string {
    return USD.format(n);
  }

  public barPct(spent: number, limit: number): number {
    return Math.min((spent / limit) * PCT_MAX, PCT_MAX);
  }

  public barColor(spent: number, limit: number, color: string): string {
    const pct = (spent / limit) * PCT_MAX;
    if (spent > limit) {
      return 'var(--color-status-error)';
    }
    if (pct >= PCT_WARNING_THRESHOLD) {
      return 'var(--color-status-warning)';
    }
    return color;
  }

  public startEdit(category: string, limit: number): void {
    this.editValue.set(String(limit));
    this.store.setEditing(category);
  }

  public saveEdit(category: string): void {
    const val = parseFloat(this.editValue());
    if (!isNaN(val) && val > 0) {
      this.store.updateLimit(category, val);
    } else {
      this.store.setEditing(null);
    }
  }

  public cancelEdit(): void {
    this.store.setEditing(null);
  }
}
