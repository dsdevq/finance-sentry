import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  AlertComponent,
  DataTableComponent,
  DonutChartComponent,
  LineChartComponent,
  StatCardComponent,
  type TableColumn,
} from '@dsdevq-common/ui';

import {type CategoryStat} from '../../models/dashboard/dashboard.model';
import {DashboardStore} from '../../store/dashboard/dashboard.store';

const CATEGORY_COLUMNS: TableColumn<CategoryStat>[] = [
  {key: 'category', header: 'Category', cell: r => r.category},
  {
    key: 'spend',
    header: 'Total Spend',
    align: 'right',
    cell: r =>
      new Intl.NumberFormat('en-US', {style: 'currency', currency: 'USD'}).format(r.totalSpend),
  },
  {
    key: 'pct',
    header: '% of Total',
    align: 'right',
    cell: r => `${r.percentOfTotal.toFixed(1)}%`,
  },
];

@Component({
  selector: 'fns-dashboard',
  imports: [
    AlertComponent,
    DataTableComponent,
    DonutChartComponent,
    LineChartComponent,
    StatCardComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DashboardStore],
  template: `
    <div class="p-cmn-6">
      <div class="mx-auto max-w-screen-xl space-y-cmn-6">
        @if (store.errorMessage()) {
          <cmn-alert variant="error">{{ store.errorMessage() }}</cmn-alert>
        }

        <div class="grid grid-cols-1 gap-cmn-4 sm:grid-cols-2 lg:grid-cols-4">
          <cmn-stat-card
            [value]="store.totalBalanceFormatted()"
            [loading]="store.isLoading()"
            label="Total Balance"
            icon="Wallet"
          />
          <cmn-stat-card
            [value]="store.data()?.accountCount?.toString() ?? '—'"
            [loading]="store.isLoading()"
            label="Accounts"
            icon="Building2"
          />
          <cmn-stat-card
            [value]="store.latestInflowFormatted()"
            [loading]="store.isLoading()"
            label="Monthly Inflow"
            icon="TrendingUp"
          />
          <cmn-stat-card
            [value]="store.latestOutflowFormatted()"
            [loading]="store.isLoading()"
            label="Monthly Outflow"
            icon="TrendingDown"
          />
        </div>

        <div class="grid grid-cols-1 gap-cmn-4 lg:grid-cols-3">
          <div class="lg:col-span-2">
            <cmn-line-chart
              [data]="store.netFlowChartData()"
              label="Monthly Net Cash Flow"
              currency="USD"
            />
          </div>
          <div>
            <cmn-donut-chart
              [segments]="store.categoryChartData()"
              label="Top Spending Categories"
              currency="USD"
            />
          </div>
        </div>

        <cmn-data-table
          [columns]="categoryColumns"
          [rows]="store.data()?.topCategories ?? []"
          emptyMessage="No spending data available"
        />
      </div>
    </div>
  `,
})
export class DashboardComponent {
  public readonly store = inject(DashboardStore);
  public readonly categoryColumns = CATEGORY_COLUMNS;
}
