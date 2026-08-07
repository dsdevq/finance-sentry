import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  input,
  OnDestroy,
  viewChild,
} from '@angular/core';
import {
  BarController,
  BarElement,
  CategoryScale,
  Chart,
  Legend,
  LinearScale,
  Tooltip,
} from 'chart.js';

import {type ChartPoint} from '../line-chart/line-chart.component';

Chart.register(CategoryScale, LinearScale, BarController, BarElement, Tooltip, Legend);

/** One bar group. Series in a chart share x labels (index-aligned). */
export interface BarSeries {
  label: string;
  color?: string;
  points: ChartPoint[];
}

export type BarValueFormat = 'currency' | 'percent';

const DEFAULT_SERIES_COLORS = ['#10b981', '#ef4444', '#6366f1', '#f59e0b', '#64748b'];
const FALLBACK_TEXT = '#464555';
const FALLBACK_GRID = '#c7c4d8';
const BAR_RADIUS = 4;

function cssVar(name: string, fallback: string): string {
  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  return value || fallback;
}

@Component({
  selector: 'cmn-bar-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex w-full flex-col gap-cmn-3 rounded-cmn-lg border border-border-default bg-surface-card p-cmn-4"
    >
      <span
        class="font-label text-cmn-xs font-semibold uppercase tracking-wide text-text-secondary"
      >
        {{ label() }}
      </span>
      <div class="relative h-64">
        <canvas #chartCanvas></canvas>
      </div>
    </div>
  `,
})
export class BarChartComponent implements AfterViewInit, OnDestroy {
  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('chartCanvas');
  private chart: Chart | null = null;

  public readonly series = input<BarSeries[]>([]);
  public readonly label = input<string>('');
  public readonly currency = input<string>('USD');
  public readonly stacked = input<boolean>(false);
  public readonly valueFormat = input<BarValueFormat>('currency');

  constructor() {
    effect(() => {
      const series = this.series();
      if (this.chart) {
        this.chart.data.labels = series[0]?.points.map(p => p.label) ?? [];
        this.chart.data.datasets = this.buildDatasets(series);
        this.chart.update('none');
      }
    });
  }

  public ngAfterViewInit(): void {
    this.buildChart();
  }

  public ngOnDestroy(): void {
    this.chart?.destroy();
    this.chart = null;
  }

  private format(value: number, compact = false): string {
    if (this.valueFormat() === 'percent') {
      return `${value.toFixed(compact ? 0 : 1)}%`;
    }
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: this.currency(),
      notation: compact ? 'compact' : 'standard',
      minimumFractionDigits: 0,
      maximumFractionDigits: compact ? 1 : 0,
    }).format(value);
  }

  private buildDatasets(series: BarSeries[]): Chart['data']['datasets'] {
    return series.map((s, i) => ({
      label: s.label,
      data: s.points.map(p => p.value),
      backgroundColor: s.color ?? DEFAULT_SERIES_COLORS[i % DEFAULT_SERIES_COLORS.length],
      borderRadius: BAR_RADIUS,
      borderSkipped: false,
      maxBarThickness: 40,
    }));
  }

  private buildChart(): void {
    const ctx = this.canvasRef().nativeElement.getContext('2d');
    if (!ctx) {
      return;
    }

    const textSecondary = cssVar('--color-text-secondary', FALLBACK_TEXT);
    const borderDefault = cssVar('--color-border-default', FALLBACK_GRID);
    const series = this.series();
    const showLegend = series.length > 1;

    this.chart = new Chart(ctx, {
      type: 'bar',
      data: {
        labels: series[0]?.points.map(p => p.label) ?? [],
        datasets: this.buildDatasets(series),
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: {mode: 'index', intersect: false},
        plugins: {
          legend: {
            display: showLegend,
            position: 'top',
            align: 'end',
            labels: {
              color: textSecondary,
              boxWidth: 10,
              boxHeight: 10,
              usePointStyle: true,
              font: {family: 'Inter', size: 11},
            },
          },
          tooltip: {
            callbacks: {
              label: tooltipCtx =>
                `${tooltipCtx.dataset.label}: ${this.format(tooltipCtx.parsed.y as number)}`,
            },
          },
        },
        scales: {
          x: {
            stacked: this.stacked(),
            grid: {display: false},
            border: {display: false},
            ticks: {
              color: textSecondary,
              font: {family: 'Inter', size: 11},
              maxRotation: 0,
              autoSkip: true,
              maxTicksLimit: 12,
            },
          },
          y: {
            stacked: this.stacked(),
            grid: {color: borderDefault},
            border: {display: false},
            ticks: {
              color: textSecondary,
              font: {family: 'Inter', size: 11},
              callback: val => this.format(val as number, true),
            },
          },
        },
      },
    });
  }
}
