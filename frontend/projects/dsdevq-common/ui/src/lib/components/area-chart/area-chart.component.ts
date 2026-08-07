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
  CategoryScale,
  Chart,
  Filler,
  Legend,
  LinearScale,
  LineController,
  LineElement,
  PointElement,
  Tooltip,
} from 'chart.js';

import {type ChartPoint} from '../line-chart/line-chart.component';

Chart.register(
  CategoryScale,
  LinearScale,
  LineController,
  PointElement,
  LineElement,
  Tooltip,
  Legend,
  Filler
);

/** One stacked band. All series in a chart are expected to share x labels (index-aligned). */
export interface AreaSeries {
  label: string;
  color?: string;
  points: ChartPoint[];
}

const DEFAULT_SERIES_COLORS = ['#10b981', '#6366f1', '#f59e0b', '#64748b', '#ec4899'];
const FILL_ALPHA = 'cc';
const FALLBACK_TEXT = '#464555';
const FALLBACK_GRID = '#c7c4d8';

function cssVar(name: string, fallback: string): string {
  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  return value || fallback;
}

function money(value: number, currency: string, compact = false): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    notation: compact ? 'compact' : 'standard',
    minimumFractionDigits: 0,
    maximumFractionDigits: compact ? 1 : 0,
  }).format(value);
}

@Component({
  selector: 'cmn-area-chart',
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
export class AreaChartComponent implements AfterViewInit, OnDestroy {
  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('chartCanvas');
  private chart: Chart | null = null;

  public readonly series = input<AreaSeries[]>([]);
  public readonly label = input<string>('');
  public readonly currency = input<string>('USD');

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

  private buildDatasets(series: AreaSeries[]): Chart['data']['datasets'] {
    return series.map((s, i) => {
      const color = s.color ?? DEFAULT_SERIES_COLORS[i % DEFAULT_SERIES_COLORS.length];
      return {
        label: s.label,
        data: s.points.map(p => p.value),
        borderColor: color,
        backgroundColor: `${color}${FILL_ALPHA}`,
        borderWidth: 1.5,
        pointRadius: 0,
        pointHoverRadius: 4,
        fill: true,
        tension: 0.3,
      };
    });
  }

  private buildChart(): void {
    const ctx = this.canvasRef().nativeElement.getContext('2d');
    if (!ctx) {
      return;
    }

    const textSecondary = cssVar('--color-text-secondary', FALLBACK_TEXT);
    const borderDefault = cssVar('--color-border-default', FALLBACK_GRID);
    const currency = this.currency();
    const series = this.series();

    this.chart = new Chart(ctx, {
      type: 'line',
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
            display: true,
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
            mode: 'index',
            intersect: false,
            callbacks: {
              label: tooltipCtx =>
                `${tooltipCtx.dataset.label}: ${money(tooltipCtx.parsed.y as number, currency)}`,
              footer: items => {
                const total = items.reduce((sum, item) => sum + (item.parsed.y as number), 0);
                return `Total: ${money(total, currency)}`;
              },
            },
          },
        },
        scales: {
          x: {
            stacked: true,
            grid: {display: false},
            border: {display: false},
            ticks: {
              color: textSecondary,
              font: {family: 'Inter', size: 11},
              maxRotation: 0,
              autoSkip: true,
              maxTicksLimit: 8,
            },
          },
          y: {
            stacked: true,
            grid: {color: borderDefault},
            border: {display: false},
            ticks: {
              color: textSecondary,
              font: {family: 'Inter', size: 11},
              callback: val => money(val as number, currency, true),
            },
          },
        },
      },
    });
  }
}
