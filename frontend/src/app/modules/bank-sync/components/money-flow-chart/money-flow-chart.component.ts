import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MonthlyFlow } from '../../services/bank-sync.service';

@Component({
  selector: 'app-money-flow-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './money-flow-chart.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MoneyFlowChartComponent {
  @Input() public monthlyFlowData: MonthlyFlow[] = [];

  public get sortedData(): MonthlyFlow[] {
    return [...this.monthlyFlowData].sort((a, b) => a.month.localeCompare(b.month));
  }

  public getMaxValue(): number {
    const values = this.monthlyFlowData.flatMap((m) => [m.inflow, m.outflow]);
    return Math.max(...values, 1);
  }

  public getBarHeightPercent(value: number): number {
    return Math.round((value / this.getMaxValue()) * 100);
  }
}
