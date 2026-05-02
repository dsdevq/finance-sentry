import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  AlertComponent,
  CmnCellDirective,
  CmnColumnComponent,
  DataTableComponent,
  DonutChartComponent,
  LineChartComponent,
  StatCardComponent,
} from '@dsdevq-common/ui';

import {AppCurrencyPipe} from '../../../../core/pipes/app-currency.pipe';
import {AppDecimalPipe} from '../../../../core/pipes/app-decimal.pipe';
import {MerchantCategoryPipe} from '../../../../shared/pipes/merchant-category.pipe';
import {DashboardStore} from '../../store/dashboard/dashboard.store';

@Component({
  selector: 'fns-dashboard',
  imports: [
    AlertComponent,
    AppCurrencyPipe,
    AppDecimalPipe,
    CmnCellDirective,
    CmnColumnComponent,
    DataTableComponent,
    DonutChartComponent,
    LineChartComponent,
    MerchantCategoryPipe,
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

        <cmn-line-chart
          [data]="store.netWorthHistoryData()"
          label="Net Worth History (13 months)"
          class="flex mt-4"
          currency="USD"
        />

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
            <ng-template let-row cmnCell
              >{{ row.percentOfTotal | appDecimal: '1.1-1' }}%</ng-template
            >
          </cmn-column>
        </cmn-data-table>
      </div>
    </div>
  `,
})
export class DashboardComponent {
  public readonly store = inject(DashboardStore);
}
