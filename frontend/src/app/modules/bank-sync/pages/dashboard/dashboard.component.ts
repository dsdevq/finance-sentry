import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {Router} from '@angular/router';
import {
  AlertComponent,
  AreaChartComponent,
  BarChartComponent,
  ButtonComponent,
  CardComponent,
  CmnCellDirective,
  CmnColumnComponent,
  DataTableComponent,
  DonutChartComponent,
  IconComponent,
  StatCardComponent,
} from '@dsdevq-common/ui';

import {AppCurrencyPipe} from '../../../../core/pipes/app-currency.pipe';
import {AppDecimalPipe} from '../../../../core/pipes/app-decimal.pipe';
import {AppRoute} from '../../../../shared/enums/app-route/app-route.enum';
import {MerchantCategoryPipe} from '../../../../shared/pipes/merchant-category.pipe';
import {type CategoryStat, type HistoryRange} from '../../models/dashboard/dashboard.model';
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
    AreaChartComponent,
    BarChartComponent,
    ButtonComponent,
    CardComponent,
    CmnCellDirective,
    CmnColumnComponent,
    DataTableComponent,
    DonutChartComponent,
    IconComponent,
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
          <p class="mt-1 text-cmn-sm text-text-secondary">How your money is trending over time</p>
        </div>

        @if (store.errorMessage()) {
          <cmn-alert variant="error">{{ store.errorMessage() }}</cmn-alert>
        }

        @if (showEmptyState()) {
          <cmn-card>
            <div class="flex flex-col items-center gap-cmn-4 py-cmn-10 text-center">
              <div
                class="flex h-16 w-16 items-center justify-center rounded-cmn-full bg-accent-subtle text-accent-default"
              >
                <cmn-icon name="Link" size="lg" />
              </div>
              <div class="space-y-cmn-2">
                <h2 class="font-headline text-cmn-xl font-semibold text-text-primary">
                  Connect your first account
                </h2>
                <p class="max-w-md text-cmn-sm text-text-secondary">
                  Finance Sentry pulls balances, transactions, and holdings from your bank,
                  brokerage, and crypto providers. Connect an account to populate this dashboard.
                </p>
              </div>
              <cmn-button (clicked)="goToAccounts()" icon="Plus">Connect Account</cmn-button>
            </div>
          </cmn-card>
        } @else {
          <div class="grid grid-cols-1 gap-cmn-4 sm:grid-cols-2 lg:grid-cols-4">
            <cmn-stat-card
              [value]="store.totalBalanceFormatted()"
              [loading]="store.isLoading()"
              label="Net Worth"
              icon="Wallet"
            />
            <button
              (click)="goToIncome()"
              type="button"
              class="w-full cursor-pointer text-left transition-opacity hover:opacity-80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent-default focus-visible:ring-offset-2"
              aria-label="View income details"
            >
              <cmn-stat-card
                [value]="store.monthlyInflowFormatted()"
                [loading]="store.isLoading()"
                label="Income this month"
                icon="TrendingUp"
              />
            </button>
            <button
              (click)="goToSpending()"
              type="button"
              class="w-full cursor-pointer text-left transition-opacity hover:opacity-80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent-default focus-visible:ring-offset-2"
              aria-label="View spending details"
            >
              <cmn-stat-card
                [value]="store.monthlySpendingFormatted()"
                [loading]="store.isLoading()"
                label="Spending this month"
                icon="TrendingDown"
              />
            </button>
            <cmn-stat-card
              [value]="store.netWorthChangeFormatted()"
              [loading]="store.isLoading()"
              label="Change (period)"
              icon="Activity"
            />
          </div>

          <div>
            <div class="mb-cmn-3 flex items-center justify-between">
              <span class="text-cmn-sm font-medium text-text-secondary">Net Worth Over Time</span>
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
              <cmn-area-chart
                [series]="store.netWorthAreaSeries()"
                label="Net worth by sleeve"
                currency="USD"
              />
              @if (store.netWorthStaleNotice()) {
                <div class="mt-cmn-2">
                  <cmn-alert variant="warning">{{ store.netWorthStaleNotice() }}</cmn-alert>
                </div>
              }
            }
          </div>

          @if (store.hasIncome()) {
            <div class="grid grid-cols-1 gap-cmn-4 lg:grid-cols-2">
              <cmn-bar-chart
                [series]="store.incomeVsSpendingBars()"
                label="Income vs Spending"
                currency="USD"
              />
              <cmn-bar-chart
                [series]="store.savingsRateBars()"
                label="Monthly Savings Rate"
                valueFormat="percent"
              />
            </div>
          } @else if (store.hasCashFlow()) {
            <cmn-bar-chart
              [series]="store.incomeVsSpendingBars()"
              label="Income vs Spending"
              currency="USD"
            />
          }

          <div class="grid grid-cols-1 gap-cmn-4 lg:grid-cols-3">
            <div>
              <cmn-donut-chart
                [segments]="store.categoryChartData()"
                label="Top Spending Categories (6M)"
                currency="USD"
              />
            </div>
            <div class="lg:col-span-2">
              <cmn-data-table
                [rows]="store.data()?.topCategories ?? []"
                (rowClick)="onCategoryClick($event)"
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
        }
      </div>
    </div>
  `,
})
export class DashboardComponent {
  private readonly router = inject(Router);

  public readonly store = inject(DashboardStore);
  public readonly ranges = HISTORY_RANGES;
  public readonly showEmptyState = computed(
    () => !this.store.isLoading() && (this.store.data()?.accountCount ?? 0) === 0
  );

  public goToAccounts(): void {
    void this.router.navigateByUrl(AppRoute.AccountsList);
  }

  public goToIncome(): void {
    void this.router.navigateByUrl(AppRoute.Income);
  }

  public goToSpending(): void {
    void this.router.navigate([AppRoute.Transactions], {queryParams: {type: 'debit'}});
  }

  public onCategoryClick(row: CategoryStat): void {
    void this.router.navigate([AppRoute.Transactions], {queryParams: {category: row.category}});
  }
}
