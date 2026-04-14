import {DecimalPipe, TitleCasePipe} from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  inject,
  OnDestroy,
  OnInit,
} from '@angular/core';
import {RouterLink} from '@angular/router';
import {AlertComponent, CardComponent} from '@dsdevq-common/ui';

import {CategoryBreakdownChartComponent} from '../../components/category-breakdown-chart/category-breakdown-chart.component';
import {MoneyFlowChartComponent} from '../../components/money-flow-chart/money-flow-chart.component';
import {BankSyncService, DashboardData} from '../../services/bank-sync.service';

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
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly bankSyncService = inject(BankSyncService);
  private readonly cdr = inject(ChangeDetectorRef);
  private refreshTimer: ReturnType<typeof setInterval> | null = null;

  public dashboardData: DashboardData | null = null;
  public isLoading = true;
  public errorMessage: string | null = null;

  public ngOnInit(): void {
    this.loadDashboard();
    // eslint-disable-next-line @typescript-eslint/no-magic-numbers
    const refreshRate = 5 * 60 * 1000; // 5 minutes
    this.refreshTimer = setInterval(() => this.loadDashboard(), refreshRate);
  }

  public ngOnDestroy(): void {
    if (this.refreshTimer) {
      clearInterval(this.refreshTimer);
    }
  }

  public loadDashboard(): void {
    this.bankSyncService.getDashboardData().subscribe({
      next: data => {
        this.dashboardData = data;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.errorMessage = 'Failed to load dashboard data. Please try again.';
        this.isLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  public getCurrencyEntries(): [string, number][] {
    return Object.entries(this.dashboardData?.aggregatedBalance ?? {});
  }

  public getAccountTypeEntries(): [string, number][] {
    return Object.entries(this.dashboardData?.accountsByType ?? {});
  }

  public getRelativeTime(ts: string | null): string {
    if (!ts) {
      return 'Never';
    }
    const diff = Date.now() - new Date(ts).getTime();
    // eslint-disable-next-line @typescript-eslint/no-magic-numbers
    const minutes = Math.floor(diff / 60000);
    if (minutes < 1) {
      return 'Just now';
    }
    // eslint-disable-next-line @typescript-eslint/no-magic-numbers
    if (minutes < 60) {
      return `${minutes}m ago`;
    }
    // eslint-disable-next-line @typescript-eslint/no-magic-numbers
    return `${Math.floor(minutes / 60)}h ago`;
  }
}
