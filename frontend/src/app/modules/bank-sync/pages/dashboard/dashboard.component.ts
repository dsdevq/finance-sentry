import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BankSyncService, DashboardData } from '../../services/bank-sync.service';
import { MoneyFlowChartComponent } from '../../components/money-flow-chart/money-flow-chart.component';
import { CategoryBreakdownChartComponent } from '../../components/category-breakdown-chart/category-breakdown-chart.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MoneyFlowChartComponent, CategoryBreakdownChartComponent],
  templateUrl: './dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit, OnDestroy {
  public dashboardData: DashboardData | null = null;
  public isLoading = true;
  public errorMessage: string | null = null;

  private readonly bankSyncService = inject(BankSyncService);
  private readonly cdr = inject(ChangeDetectorRef);
  private refreshTimer: ReturnType<typeof setInterval> | null = null;

  public ngOnInit(): void {
    this.loadDashboard();
    this.refreshTimer = setInterval(() => this.loadDashboard(), 5 * 60 * 1000);
  }

  public ngOnDestroy(): void {
    if (this.refreshTimer) clearInterval(this.refreshTimer);
  }

  public loadDashboard(): void {
    this.bankSyncService.getDashboardData().subscribe({
      next: (data) => {
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
    if (!ts) return 'Never';
    const diff = Date.now() - new Date(ts).getTime();
    const minutes = Math.floor(diff / 60000);
    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;
    return `${Math.floor(minutes / 60)}h ago`;
  }
}
