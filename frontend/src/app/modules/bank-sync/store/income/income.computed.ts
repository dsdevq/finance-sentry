import {CurrencyPipe} from '@angular/common';
import {computed, inject, type Signal} from '@angular/core';
import {type BarSeries} from '@dsdevq-common/ui';

import {type MonthlyFlow} from '../../models/dashboard/dashboard.model';
import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';

interface StateSignals {
  monthlyFlow: Signal<MonthlyFlow[]>;
  transactions: Signal<GlobalTransactionDto[]>;
  totalCount: Signal<number>;
  hasMore: Signal<boolean>;
}

const MONTH_FORMATTER = new Intl.DateTimeFormat('en-US', {month: 'short'});
const YEAR_SUFFIX_DIGITS = 2;
const INCOME_COLOR = '#10b981';

function formatMonthYear(date: Date): string {
  return `${MONTH_FORMATTER.format(date)} '${String(date.getUTCFullYear()).slice(-YEAR_SUFFIX_DIGITS)}`;
}

function formatMonthKey(key: string): string {
  const [year, month] = key.split('-').map(Number);
  return formatMonthYear(new Date(Date.UTC(year, month - 1, 1)));
}

function currentMonthKey(): string {
  const now = new Date();
  const MONTH_KEY_PAD = 2;
  return `${now.getUTCFullYear()}-${String(now.getUTCMonth() + 1).padStart(MONTH_KEY_PAD, '0')}`;
}

export function incomeComputed(store: StateSignals) {
  const currency = inject(CurrencyPipe);

  const flowByMonth = computed(() => {
    const byMonth = new Map<string, number>();
    for (const r of store.monthlyFlow()) {
      byMonth.set(r.month, (byMonth.get(r.month) ?? 0) + r.inflowUsd);
    }
    return byMonth;
  });

  const currentMonthInflow = computed((): number => flowByMonth().get(currentMonthKey()) ?? 0);

  const averageMonthlyIncome = computed((): number => {
    const values = [...flowByMonth().values()].filter(v => v > 0);
    return values.length > 0 ? values.reduce((a, b) => a + b, 0) / values.length : 0;
  });

  const ytdIncome = computed((): number => {
    const currentYear = new Date().getUTCFullYear();
    let total = 0;
    for (const [key, value] of flowByMonth().entries()) {
      if (key.startsWith(`${currentYear}-`)) {
        total += value;
      }
    }
    return total;
  });

  return {
    thisMonthFormatted: computed(
      () => currency.transform(currentMonthInflow(), 'USD', 'symbol', '1.0-0') ?? '—'
    ),
    avgMonthlyFormatted: computed(
      () => currency.transform(averageMonthlyIncome(), 'USD', 'symbol', '1.0-0') ?? '—'
    ),
    ytdFormatted: computed(() => currency.transform(ytdIncome(), 'USD', 'symbol', '1.0-0') ?? '—'),

    monthlyIncomeBars: computed((): BarSeries[] => {
      const sorted = [...flowByMonth().entries()].sort(([a], [b]) => a.localeCompare(b));
      if (sorted.length === 0) {
        return [];
      }
      return [
        {
          label: 'Income',
          color: INCOME_COLOR,
          points: sorted.map(([key, value]) => ({label: formatMonthKey(key), value})),
        },
      ];
    }),

    hasFlow: computed(() => store.monthlyFlow().length > 0),
    isEmpty: computed(() => store.transactions().length === 0),
  };
}
