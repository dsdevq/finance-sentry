import {CommonModule} from '@angular/common';
import {ChangeDetectionStrategy, Component, input} from '@angular/core';
import {CategoryStat} from '@modules/bank-sync/models/dashboard/dashboard.model';

@Component({
  selector: 'fns-category-breakdown-chart',
  imports: [CommonModule],
  templateUrl: './category-breakdown-chart.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoryBreakdownChartComponent {
  public readonly topCategoriesData = input<CategoryStat[]>([]);

  public get sortedData(): CategoryStat[] {
    return [...this.topCategoriesData()].sort((a, b) => b.totalSpend - a.totalSpend);
  }

  public getBarWidthPercent(percentOfTotal: number): number {
    const maxPercent = 100;
    return Math.min(Math.round(percentOfTotal), maxPercent);
  }
}
