import {CommonModule} from '@angular/common';
import {ChangeDetectionStrategy, Component, input} from '@angular/core';

import {MonthlyFlow} from '../../services/bank-sync.service';

@Component({
  selector: 'fns-money-flow-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './money-flow-chart.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MoneyFlowChartComponent {
  public readonly monthlyFlowData = input<MonthlyFlow[]>([]);

  public get sortedData(): MonthlyFlow[] {
    return [...this.monthlyFlowData()].sort((a, b) => a.month.localeCompare(b.month));
  }

  public getMaxValue(): number {
    const values = this.monthlyFlowData().flatMap(m => [m.inflow, m.outflow]);
    return Math.max(...values, 1);
  }

  public getBarHeightPercent(value: number): number {
    const percent = 100;
    return Math.round((value / this.getMaxValue()) * percent);
  }
}
