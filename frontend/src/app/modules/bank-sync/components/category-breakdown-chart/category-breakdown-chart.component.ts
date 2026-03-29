import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CategoryStat } from '../../services/bank-sync.service';

@Component({
  selector: 'app-category-breakdown-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './category-breakdown-chart.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoryBreakdownChartComponent {
  @Input() topCategoriesData: CategoryStat[] = [];

  get sortedData(): CategoryStat[] {
    return [...this.topCategoriesData].sort((a, b) => b.totalSpend - a.totalSpend);
  }

  getBarWidthPercent(percentOfTotal: number): number {
    return Math.min(Math.round(percentOfTotal), 100);
  }
}
