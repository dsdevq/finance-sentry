import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {
  AlertComponent,
  ButtonComponent,
  CmnCellDirective,
  CmnColumnComponent,
  DataTableComponent,
  DonutChartComponent,
  LineChartComponent,
  type LineChartConfig,
  StatCardComponent,
} from '@dsdevq-common/ui';

import {AppCurrencyPipe} from '../../../../core/pipes/app-currency.pipe';
import {AppDecimalPipe} from '../../../../core/pipes/app-decimal.pipe';
import {MerchantCategoryPipe} from '../../../../shared/pipes/merchant-category.pipe';
import {NetWorthChartComponent} from '../../components/net-worth-chart/net-worth-chart.component';
import {type HistoryRange} from '../../models/dashboard/dashboard.model';
import {DashboardStore} from '../../store/dashboard/dashboard.store';

const HISTORY_RANGES: {label: string; value: HistoryRange}[] = [
  {label: '3M', value: '3m'},
  {label: '6M', value: '6m'},
  {label: '1Y', value: '1y'},
  {label: 'All', value: 'all'},
];

@Component({
  selector: 'fns-dashboard',
  imports: [
    AlertComponent,
    AppCurrencyPipe,
    AppDecimalPipe,
    ButtonComponent,
    CmnCellDirective,
    CmnColumnComponent,
    DataTableComponent,
    DonutChartComponent,
    LineChartComponent,
    MerchantCategoryPipe,
    NetWorthChartComponent,
    StatCardComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DashboardStore],
  template: `
    <div class="p-cmn-6">
      <div class="mx-auto max-w-screen-lg space-y-cmn-6">
        <div>
          <h1 class="font-headline text-2xl font-bold text-text-primary">Dashboard</h1>
          <p class="mt-1 text-cmn-sm text-text-secondary">Your financial overview at a glance</p>
        </div>

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

        <div>
          <div class="mb-cmn-3 flex items-center justify-between">
            <span class="text-cmn-sm font-medium text-text-secondary">Net Worth History</span>
            <div class="flex gap-cmn-1">
              @for (r of ranges; track r.value) {
                <cmn-button
                  [variant]="store.historyRange() === r.value ? 'primary' : 'secondary'"
                  (clicked)="store.setHistoryRange(r.value)"
                  size="sm"
                  >{{ r.label }}</cmn-button
                >
              }
            </div>
          </div>

          @if (store.historyErrorMessage()) {
            <cmn-alert variant="error">{{ store.historyErrorMessage() }}</cmn-alert>
          } @else if (!store.historyHasHistory() && !store.isHistoryLoading()) {
            <cmn-alert variant="info"
              >No history yet. Run the net worth snapshot job to populate the chart.</cmn-alert
            >
          } @else {
            <fns-net-worth-chart [series]="store.netWorthSeriesData()" currency="USD" />
          }
        </div>

        <div class="grid grid-cols-1 gap-cmn-4 lg:grid-cols-3">
          <div class="lg:col-span-2">
            <cmn-line-chart [config]="netFlowConfig()" />
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
          [rows]="store.data()?.topCategories ?? []"
          class="grid gap-4"
          emptyMessage="No spending data available"
        >
          <cmn-column key="category" header="Category">
            <ng-template let-row cmnCell>{{ row.category | merchantCategory }}</ng-template>
          </cmn-column>
          <cmn-column key="spend" header="Total Spend" align="right">
            <ng-template let-row cmnCell>{{ row.totalSpend | appCurrency }}</ng-template>
          </cmn-column>
          <cmn-column key="pct" header="% of Total" align="right">
            <ng-template let-row cmnCell>
              <div class="flex items-center justify-end gap-cmn-2">
                <div class="h-1.5 w-20 overflow-hidden rounded-full bg-surface-raised">
                  <div
                    [style.width.%]="row.percentOfTotal"
                    class="h-full rounded-full bg-accent-default"
                  ></div>
                </div>
                <span class="font-mono text-cmn-xs"
                  >{{ row.percentOfTotal | appDecimal: '1.1-1' }}%</span
                >
              </div>
            </ng-template>
          </cmn-column>
        </cmn-data-table>
      </div>
    </div>
  `,
})
export class DashboardComponent {
  public readonly store = inject(DashboardStore);
  public readonly ranges = HISTORY_RANGES;

  public readonly netFlowConfig = computed(
    (): LineChartConfig => ({
      data: this.store.netFlowChartData(),
      label: 'Monthly Net Cash Flow',
      currency: 'USD',
    })
  );
}
