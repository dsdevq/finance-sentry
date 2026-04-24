import {DecimalPipe, TitleCasePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AlertComponent, CardComponent} from '@dsdevq-common/ui';

import {getRelativeTime} from '../../../../shared/utils/relative-time';
import {CategoryBreakdownChartComponent} from '../../components/category-breakdown-chart/category-breakdown-chart.component';
import {MoneyFlowChartComponent} from '../../components/money-flow-chart/money-flow-chart.component';
import {DashboardStore} from '../../store/dashboard/dashboard.store';

@Component({
  selector: 'fns-dashboard',
  standalone: true,
  imports: [
    RouterLink,
    AlertComponent,
    CardComponent,
    DecimalPipe,
    MoneyFlowChartComponent,
    CategoryBreakdownChartComponent,
    TitleCasePipe,
  ],
  templateUrl: './dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DashboardStore],
})
export class DashboardComponent {
  public readonly store = inject(DashboardStore);
  public readonly getRelativeTime = getRelativeTime;
}
