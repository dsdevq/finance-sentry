import {DatePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  AlertComponent,
  BarChartComponent,
  ButtonComponent,
  CardComponent,
  EmptyStateComponent,
  SkeletonComponent,
  StatCardComponent,
} from '@lifekit-hq/ui';

import {TransactionAmountPipe} from '../../pipes/transaction-amount.pipe';
import {TransactionAmountClassPipe} from '../../pipes/transaction-amount-class.pipe';
import {IncomeStore} from '../../store/income/income.store';

const SKELETON_ROWS = 8;

@Component({
  selector: 'fns-income',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AlertComponent,
    BarChartComponent,
    ButtonComponent,
    CardComponent,
    DatePipe,
    EmptyStateComponent,
    SkeletonComponent,
    StatCardComponent,
    TransactionAmountClassPipe,
    TransactionAmountPipe,
  ],
  providers: [IncomeStore],
  template: `
    <div class="mx-auto flex h-full w-full max-w-[1200px] flex-col p-cmn-8">
      <div class="mb-cmn-8">
        <h1 class="font-headline text-cmn-2xl font-bold text-text-primary">Income</h1>
        <p class="mt-cmn-1 text-cmn-sm text-text-secondary">
          Income transactions across connected accounts
        </p>
      </div>

      @if (store.errorMessage()) {
        <cmn-alert variant="error" class="mb-cmn-6 block">
          {{ store.errorMessage() }}
          <cmn-button (clicked)="store.load()" variant="secondary" size="sm">Retry</cmn-button>
        </cmn-alert>
      }

      <div class="mb-cmn-8 grid grid-cols-1 gap-cmn-4 sm:grid-cols-3">
        <cmn-stat-card
          [value]="store.thisMonthFormatted()"
          [loading]="store.isLoading()"
          label="Income this month"
          icon="TrendingUp"
        />
        <cmn-stat-card
          [value]="store.avgMonthlyFormatted()"
          [loading]="store.isLoading()"
          label="Monthly average"
          icon="ChartNoAxesColumn"
        />
        <cmn-stat-card
          [value]="store.ytdFormatted()"
          [loading]="store.isLoading()"
          label="Year to date"
          icon="Calendar"
        />
      </div>

      @if (store.hasFlow()) {
        <div class="mb-cmn-8">
          <cmn-bar-chart
            [series]="store.monthlyIncomeBars()"
            label="Monthly Income"
            currency="USD"
          />
        </div>
      }

      @if (store.isLoading()) {
        <cmn-card class="min-h-0 flex-1">
          <div class="space-y-cmn-3 p-cmn-4">
            @for (_ of skeletonRows; track $index) {
              <div class="flex gap-cmn-4">
                <cmn-skeleton height="1rem" width="15%" />
                <cmn-skeleton height="1rem" width="30%" />
                <cmn-skeleton height="1rem" width="20%" />
                <cmn-skeleton height="1rem" width="20%" />
                <cmn-skeleton height="1rem" width="15%" />
              </div>
            }
          </div>
        </cmn-card>
      } @else if (store.isEmpty()) {
        <div class="flex min-h-0 flex-1 items-center justify-center">
          <cmn-empty-state
            message="No income transactions found"
            subMessage="Connect an account to see your income history."
          />
        </div>
      } @else {
        <cmn-card [fill]="true" padding="none" class="min-h-0 flex-1">
          <div class="min-h-0 flex-1 overflow-auto">
            <table class="w-full text-cmn-sm" aria-label="Income transactions">
              <thead class="sticky top-0 z-10">
                <tr
                  class="border-b border-border-default bg-surface-card text-cmn-xs uppercase tracking-wide text-text-secondary"
                >
                  <th class="px-cmn-4 py-cmn-3 text-left font-medium" scope="col">Date</th>
                  <th class="px-cmn-4 py-cmn-3 text-left font-medium" scope="col">Description</th>
                  <th class="px-cmn-4 py-cmn-3 text-left font-medium" scope="col">Account</th>
                  <th class="px-cmn-4 py-cmn-3 text-right font-medium" scope="col">Amount</th>
                </tr>
              </thead>
              <tbody>
                @for (t of store.transactions(); track t.transactionId) {
                  <tr
                    class="border-b border-border-default last:border-0 transition-colors hover:bg-surface-bg"
                  >
                    <td class="whitespace-nowrap px-cmn-4 py-cmn-4 text-text-secondary">
                      {{ t.postedDate ?? t.date | date: 'MMM d, y' }}
                    </td>
                    <td class="max-w-[300px] truncate px-cmn-4 py-cmn-4 text-text-primary">
                      {{ t.description }}
                    </td>
                    <td class="px-cmn-4 py-cmn-4 text-text-secondary">{{ t.bankName }}</td>
                    <td
                      [class]="t | transactionAmountClass"
                      class="px-cmn-4 py-cmn-4 text-right font-mono tabular-nums font-medium"
                    >
                      {{ t | transactionAmount }}
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          @if (store.hasMore()) {
            <div class="flex justify-center border-t border-border-default p-cmn-4">
              <cmn-button (clicked)="store.loadMore()" variant="secondary" size="sm"
                >Load More</cmn-button
              >
            </div>
          }
        </cmn-card>
      }
    </div>
  `,
})
export class IncomeComponent {
  public readonly store = inject(IncomeStore);
  public readonly skeletonRows = Array.from({length: SKELETON_ROWS});
}
