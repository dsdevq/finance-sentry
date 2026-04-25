import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {toSignal} from '@angular/core/rxjs-interop';
import {Router} from '@angular/router';
import {
  AlertComponent,
  DataTableComponent,
  DonutChartComponent,
  LineChartComponent,
  type NavItem,
  SidebarNavComponent,
  StatCardComponent,
  type TableColumn,
  ThemeService,
  TopBarComponent,
} from '@dsdevq-common/ui';

import {AppRoute} from '../../../../shared/enums/app-route.enum';
import {type CategoryStat} from '../../models/dashboard.model';
import {DashboardStore} from '../../store/dashboard/dashboard.store';

const NAV_ITEMS: NavItem[] = [
  {label: 'Dashboard', icon: 'LayoutDashboard', route: AppRoute.Dashboard},
  {label: 'Accounts', icon: 'Building2', route: AppRoute.AccountsList},
];

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
  standalone: true,
  imports: [
    AlertComponent,
    DataTableComponent,
    DonutChartComponent,
    LineChartComponent,
    SidebarNavComponent,
    StatCardComponent,
    TopBarComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DashboardStore],
  template: `
    <div class="flex h-screen overflow-hidden bg-surface-bg">
      <!-- Sidebar -->
      <cmn-sidebar-nav
        [items]="navItems"
        [activeRoute]="AppRoute.Dashboard"
        (navClick)="navigate($event)"
      />

      <!-- Main area -->
      <div class="flex flex-1 flex-col overflow-hidden">
        <cmn-top-bar
          [isDark]="isDark()"
          (themeToggle)="themeService.toggle()"
          title="Dashboard"
          avatarLabel="D"
        />

        <main class="flex-1 overflow-y-auto p-cmn-6">
          <div class="mx-auto max-w-screen-xl space-y-cmn-6">
            @if (store.errorMessage()) {
              <cmn-alert variant="error">{{ store.errorMessage() }}</cmn-alert>
            }

            <!-- KPI row -->
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

            <!-- Charts row -->
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

            <!-- Categories table -->
            <cmn-data-table
              [columns]="categoryColumns"
              [rows]="store.data()?.topCategories ?? []"
              emptyMessage="No spending data available"
            />
          </div>
        </main>
      </div>
    </div>
  `,
})
export class DashboardComponent {
  private readonly router = inject(Router);
  private readonly theme = toSignal(inject(ThemeService).activeTheme$, {
    initialValue: 'light' as const,
  });

  public readonly store = inject(DashboardStore);
  public readonly themeService = inject(ThemeService);
  public readonly isDark = computed(() => this.theme() === 'dark');
  public readonly navItems = NAV_ITEMS;
  public readonly categoryColumns = CATEGORY_COLUMNS;
  public readonly AppRoute = AppRoute;

  public navigate(item: NavItem): void {
    void this.router.navigateByUrl(item.route);
  }
}
