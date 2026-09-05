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
} from '@lifekit-hq/ui';

import {AppCurrencyPipe} from '../../../../core/pipes/app-currency.pipe';
import {AppDecimalPipe} from '../../../../core/pipes/app-decimal.pipe';
import {AppRoute} from '../../../../shared/enums/app-route/app-route.enum';
import {MerchantCategoryPipe} from '../../../../shared/pipes/merchant-category.pipe';
import {
  HISTORY_RANGE_LABELS,
  PROJECTION_RETURN_RATES,
} from '../../constants/dashboard/dashboard.constants';
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
          <div class="grid grid-cols-1 gap-cmn-4 sm:grid-cols-2">
            <cmn-stat-card
              [value]="store.totalBalanceFormatted()"
              [loading]="store.isLoading()"
              label="Net Worth"
              icon="Wallet"
            />
            <cmn-stat-card
              [value]="store.netWorthChangeFormatted()"
              [loading]="store.isLoading()"
              label="Change (period)"
              icon="Activity"
            />
          </div>

          <!--
            The in-progress month lives here and nowhere else. The charts below plot closed
            months only, so a partial figure never sits next to a complete one pretending to
            be comparable; here it is labelled month-to-date and paced against the trailing
            complete months, which is the comparison that actually means something mid-month.
          -->
          <div>
            <div class="mb-cmn-3 flex items-baseline justify-between">
              <span class="text-cmn-sm font-medium text-text-secondary">This month</span>
              <span class="text-cmn-xs text-text-disabled">
                Month to date, paced against the last 3 complete months
              </span>
            </div>
            <div class="grid grid-cols-1 gap-cmn-4 sm:grid-cols-3">
              <button
                (click)="goToIncome()"
                type="button"
                class="w-full cursor-pointer text-left transition-opacity hover:opacity-80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent-default focus-visible:ring-offset-2"
                aria-label="View income details"
              >
                <cmn-stat-card
                  [value]="store.monthlyInflowFormatted()"
                  [delta]="store.inflowPaceDelta()"
                  [deltaLabel]="store.inflowPaceLabel()"
                  [loading]="store.isLoading()"
                  label="Income (MTD)"
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
                  [delta]="store.spendingPaceDelta()"
                  [deltaLabel]="store.spendingPaceLabel()"
                  [loading]="store.isLoading()"
                  label="Spending (MTD)"
                  icon="TrendingDown"
                />
              </button>
              <cmn-stat-card
                [value]="store.savingsRateMonthToDateFormatted()"
                [delta]="store.savingsRatePaceDelta()"
                [deltaLabel]="store.savingsRatePaceLabel()"
                [loading]="store.isLoading()"
                label="Savings rate (MTD)"
                icon="PiggyBank"
              />
            </div>
          </div>

          @if (store.hasFlowBreakdown()) {
            <div>
              <div class="mb-cmn-3">
                <span class="text-cmn-sm font-medium text-text-secondary"
                  >Flow breakdown (MTD)</span
                >
              </div>
              <div class="grid grid-cols-1 gap-cmn-4 sm:grid-cols-2 lg:grid-cols-4">
                <cmn-stat-card
                  [value]="store.monthlySpentFormatted()"
                  [loading]="store.isLoading()"
                  label="Spent"
                  icon="ShoppingBag"
                />
                <cmn-stat-card
                  [value]="store.monthlyFamilySupportFormatted()"
                  [loading]="store.isLoading()"
                  label="Supported family"
                  icon="Heart"
                />
                <cmn-stat-card
                  [value]="store.monthlyInvestedFormatted()"
                  [loading]="store.isLoading()"
                  label="Invested"
                  icon="TrendingUp"
                />
                <cmn-stat-card
                  [value]="store.monthlyKeptFormatted()"
                  [loading]="store.isLoading()"
                  label="Kept"
                  icon="Banknote"
                />
              </div>
            </div>
          }

          <!--
            Projected from contributions, never from the net-worth line: most of the book is
            market-marked, so a trend fitted to that line would forecast the market and call it
            a savings forecast. Market return is a separate assumption the reader chooses and
            the tile spells out. Hidden below three complete months by hasProjection().
          -->
          @if (store.hasProjection()) {
            <div>
              <div class="mb-cmn-3 flex flex-wrap items-baseline justify-between gap-cmn-2">
                <span class="text-cmn-sm font-medium text-text-secondary">
                  Where this is heading
                </span>
                <div class="flex items-center gap-cmn-2">
                  <span class="text-cmn-xs text-text-disabled">Assumed market return</span>
                  <div class="flex gap-cmn-1">
                    @for (rate of returnRates; track rate.value) {
                      <cmn-button
                        [variant]="
                          store.projectionReturnRate() === rate.value ? 'primary' : 'secondary'
                        "
                        (clicked)="store.setProjectionReturnRate(rate.value)"
                        size="sm"
                        >{{ rate.label }}</cmn-button
                      >
                    }
                  </div>
                </div>
              </div>
              <cmn-card>
                <div class="grid grid-cols-1 gap-cmn-4 sm:grid-cols-2">
                  <div class="space-y-cmn-1">
                    <p class="text-cmn-xs text-text-secondary">Projected net worth in 12 months</p>
                    <p class="font-headline text-2xl font-bold text-text-primary">
                      {{ store.projectedNetWorthFormatted() }}
                    </p>
                    <p class="text-cmn-xs text-text-disabled">
                      {{ store.projectionAssumptionLabel() }}
                    </p>
                  </div>
                  <div class="space-y-cmn-1">
                    <p class="text-cmn-xs text-text-secondary">Median monthly savings</p>
                    <p class="font-headline text-2xl font-bold text-text-primary">
                      {{ store.medianMonthlySavingsFormatted() }}
                    </p>
                    <p class="text-cmn-xs text-text-disabled">
                      {{ store.projectionBasisLabel() }}
                    </p>
                  </div>
                </div>

                <!--
                  The headline split into the addends that make it. Only the market-return
                  line moves with the rate toggle, so "held separate" is something the reader
                  can check against the number rather than a claim in the prose above.
                -->
                <dl
                  class="mt-cmn-4 grid grid-cols-3 gap-cmn-2 border-t border-border-default pt-cmn-3 text-cmn-xs"
                >
                  <div>
                    <dt class="text-text-secondary">Today</dt>
                    <dd class="font-medium text-text-primary">
                      {{ store.projectionTodayFormatted() }}
                    </dd>
                  </div>
                  <div>
                    <dt class="text-text-secondary">Contributions</dt>
                    <dd class="font-medium text-text-primary">
                      {{ store.projectedContributionsFormatted() }}
                    </dd>
                  </div>
                  <div>
                    <dt class="text-text-secondary">Market return</dt>
                    <dd class="font-medium text-text-primary">
                      {{ store.projectedMarketReturnFormatted() }}
                    </dd>
                  </div>
                </dl>
              </cmn-card>
            </div>
          }

          <div>
            <div class="mb-cmn-3 flex items-center justify-between">
              <span class="text-cmn-sm font-medium text-text-secondary">Net Worth Over Time</span>
              <div class="flex items-center gap-cmn-3">
                <div class="flex gap-cmn-1">
                  <cmn-button
                    [variant]="store.netWorthStacked() ? 'primary' : 'secondary'"
                    (clicked)="store.setNetWorthStacked(true)"
                    size="sm"
                    >Stacked</cmn-button
                  >
                  <cmn-button
                    [variant]="store.netWorthStacked() ? 'secondary' : 'primary'"
                    (clicked)="store.setNetWorthStacked(false)"
                    size="sm"
                    >Lines</cmn-button
                  >
                </div>
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
                [stacked]="store.netWorthStacked()"
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
                label="Income vs Spending (complete months)"
                currency="USD"
              />
              <cmn-bar-chart
                [series]="store.savingsRateBars()"
                label="Monthly Savings Rate (complete months)"
                valueFormat="percent"
              />
            </div>
          } @else if (store.hasCashFlow()) {
            <cmn-bar-chart
              [series]="store.incomeVsSpendingBars()"
              label="Income vs Spending (complete months)"
              currency="USD"
            />
          }

          <div class="grid grid-cols-1 gap-cmn-4 lg:grid-cols-3">
            <div>
              <cmn-donut-chart
                [segments]="store.categoryChartData()"
                [label]="topCategoriesLabel()"
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
  public readonly returnRates = PROJECTION_RETURN_RATES;
  public readonly showEmptyState = computed(
    () => !this.store.isLoading() && (this.store.data()?.accountCount ?? 0) === 0
  );
  public readonly topCategoriesLabel = computed(
    () => `Top Spending Categories (${HISTORY_RANGE_LABELS[this.store.historyRange()]})`
  );

  public goToAccounts(): void {
    void this.router.navigateByUrl(AppRoute.AccountsList);
  }

  public goToIncome(): void {
    void this.router.navigate([AppRoute.Transactions], {queryParams: {type: 'credit'}});
  }

  public goToSpending(): void {
    void this.router.navigate([AppRoute.Transactions], {queryParams: {type: 'debit'}});
  }

  public onCategoryClick(row: CategoryStat): void {
    void this.router.navigate([AppRoute.Transactions], {queryParams: {category: row.category}});
  }
}
