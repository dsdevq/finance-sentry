import {CurrencyPipe} from '@angular/common';
import {provideHttpClient} from '@angular/common/http';
import {provideHttpClientTesting} from '@angular/common/http/testing';
import {signal} from '@angular/core';
import {TestBed} from '@angular/core/testing';
import {provideApiBaseUrl} from '@lifekit-hq/core';
import {afterEach, beforeEach, describe, expect, it, vi} from 'vitest';

import {
  type DashboardData,
  type MonthlyFlow,
  type NetWorthSnapshotDto,
} from '../../models/dashboard/dashboard.model';
import {dashboardComputed} from './dashboard.computed';

// Frozen mid-month so "the current month" is genuinely partial: 12 of 31 days elapsed.
// Every pace assertion below is anchored to that 12/31 fraction.
const NOW = new Date('2026-08-12T00:00:00.000Z');
const ELAPSED_FRACTION = 12 / 31;

function flow(month: string, inflow: number, outflow: number): MonthlyFlow {
  return {
    month,
    currency: 'USD',
    inflow,
    outflow,
    net: inflow - outflow,
    inflowUsd: inflow,
    outflowUsd: outflow,
    netUsd: inflow - outflow,
  };
}

function build(monthlyFlow: MonthlyFlow[]) {
  const data: DashboardData = {
    aggregatedBalance: {USD: 0},
    totalNetWorthUsd: 0,
    accountCount: 1,
    accountsByType: {},
    monthlyFlow,
    topCategories: [],
    lastSyncTimestamp: null,
  } as unknown as DashboardData;

  return {
    data: signal<Nullable<DashboardData>>(data),
    netWorthHistory: signal<NetWorthSnapshotDto[]>([]),
    historyLoading: signal(false),
    historyError: signal<string | null>(null),
  };
}

function computedFor(monthlyFlow: MonthlyFlow[]) {
  return TestBed.runInInjectionContext(() => dashboardComputed(build(monthlyFlow)));
}

/** Three closed months at a steady 4000 in / 2000 out, plus a partial August. */
function steadyHistory(augustInflow: number, augustOutflow: number): MonthlyFlow[] {
  return [
    flow('2026-05', 4000, 2000),
    flow('2026-06', 4000, 2000),
    flow('2026-07', 4000, 2000),
    flow('2026-08', augustInflow, augustOutflow),
  ];
}

describe('dashboardComputed', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(NOW);
    TestBed.configureTestingModule({
      providers: [
        CurrencyPipe,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideApiBaseUrl('http://localhost/api/v1'),
      ],
    });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('charts plot complete calendar months only', () => {
    it('keeps the in-progress month out of income vs spending', () => {
      const bars = computedFor(steadyHistory(500, 900)).incomeVsSpendingBars();

      expect(bars.map(s => s.label)).toEqual(['Income', 'Spending']);
      for (const series of bars) {
        expect(series.points.map(p => p.label)).toEqual(["May '26", "Jun '26", "Jul '26"]);
      }
    });

    it('gives income vs spending and savings rate the same x-axis', () => {
      const c = computedFor(steadyHistory(500, 900));

      const incomeMonths = c.incomeVsSpendingBars()[0].points.map(p => p.label);
      const savingsMonths = c.savingsRateBars()[0].points.map(p => p.label);

      expect(savingsMonths).toEqual(incomeMonths);
    });

    it('still drops closed months that had no income at all', () => {
      // A zero-inflow month sends net/inflow to absurd magnitudes; it is not a savings rate.
      const c = computedFor([flow('2026-06', 0, 800), flow('2026-07', 4000, 2000)]);

      expect(c.savingsRateBars()[0].points.map(p => p.label)).toEqual(["Jul '26"]);
    });

    it('computes the closed-month savings rate from net over inflow', () => {
      const c = computedFor(steadyHistory(500, 900));

      expect(c.savingsRateBars()[0].points[0].value).toBeCloseTo(50, 5);
    });

    it('reports no chartable data when only the in-progress month exists', () => {
      const c = computedFor([flow('2026-08', 500, 900)]);

      expect(c.hasCashFlow()).toBe(false);
      expect(c.hasIncome()).toBe(false);
      expect(c.incomeVsSpendingBars()).toEqual([]);
      expect(c.savingsRateBars()).toEqual([]);
    });
  });

  describe('month-to-date tiles carry the in-progress month', () => {
    it('reports month-to-date income and spending, not the closed months', () => {
      const c = computedFor(steadyHistory(500, 900));

      expect(c.monthlyInflowFormatted()).toBe('$500');
      expect(c.monthlySpendingFormatted()).toBe('$900');
    });

    it('falls back to an em dash when the current month has no rows yet', () => {
      const c = computedFor([flow('2026-07', 4000, 2000)]);

      expect(c.monthlyInflowFormatted()).toBe('—');
      expect(c.monthlySpendingFormatted()).toBe('—');
    });

    it('paces spending against the prorated average of the closed months', () => {
      // Exactly on pace: 2000 * 12/31 ≈ 774.
      const onPace = computedFor(steadyHistory(4000, 2000 * ELAPSED_FRACTION));

      expect(onPace.spendingPaceLabel()).toBe('0% over pace');
    });

    it('colours overspending red by inverting the delta the card reads', () => {
      const c = computedFor(steadyHistory(4000, 2000 * ELAPSED_FRACTION * 1.5));

      expect(c.spendingPaceLabel()).toBe('50% over pace');
      // The card renders delta >= 0 as green; spending above pace must not be green.
      expect(c.spendingPaceDelta()).toBeLessThan(0);
    });

    it('colours underspending green', () => {
      const c = computedFor(steadyHistory(4000, 2000 * ELAPSED_FRACTION * 0.5));

      expect(c.spendingPaceLabel()).toBe('50% under pace');
      expect(c.spendingPaceDelta()).toBeGreaterThan(0);
    });

    it('colours income above pace green and below pace red', () => {
      const ahead = computedFor(steadyHistory(4000 * ELAPSED_FRACTION * 1.2, 0));
      expect(ahead.inflowPaceLabel()).toBe('20% above pace');
      expect(ahead.inflowPaceDelta()).toBeGreaterThan(0);

      const behind = computedFor(steadyHistory(4000 * ELAPSED_FRACTION * 0.7, 0));
      expect(behind.inflowPaceLabel()).toBe('30% below pace');
      expect(behind.inflowPaceDelta()).toBeLessThan(0);
    });

    it('shows no pace chip when there are no closed months to compare against', () => {
      const c = computedFor([flow('2026-08', 500, 900)]);

      expect(c.inflowPaceDelta()).toBeNull();
      expect(c.spendingPaceDelta()).toBeNull();
      expect(c.inflowPaceLabel()).toBe('');
    });
  });

  describe('month-to-date savings rate waits for income to land', () => {
    it('withholds the rate while this month is mostly spending against stray credits', () => {
      // $200 against a normal $4000 month — salary has not posted, so the raw rate would
      // read about -350% and mean nothing.
      const c = computedFor(steadyHistory(200, 900));

      expect(c.savingsRateMonthToDateFormatted()).toBe('—');
      expect(c.savingsRatePaceDelta()).toBeNull();
    });

    it('reports the rate once income has substantially landed', () => {
      const c = computedFor(steadyHistory(4000, 1000));

      expect(c.savingsRateMonthToDateFormatted()).toBe('75%');
    });

    it('compares the rate in percentage points against the usual closed months', () => {
      // Closed months run at 50%; this month is at 75%.
      const c = computedFor(steadyHistory(4000, 1000));

      expect(c.savingsRatePaceLabel()).toBe('25 pts above usual');
      expect(c.savingsRatePaceDelta()).toBeGreaterThan(0);
    });

    it('flags a rate below the usual months as negative', () => {
      const c = computedFor(steadyHistory(4000, 3000));

      expect(c.savingsRatePaceLabel()).toBe('25 pts below usual');
      expect(c.savingsRatePaceDelta()).toBeLessThan(0);
    });

    it('withholds the rate when the current month has no rows at all', () => {
      const c = computedFor([flow('2026-07', 4000, 2000)]);

      expect(c.savingsRateMonthToDateFormatted()).toBe('—');
    });
  });
});
