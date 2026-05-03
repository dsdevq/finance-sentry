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
  LinearScale,
  LineController,
  LineElement,
  PointElement,
  Tooltip,
} from 'chart.js';

Chart.register(
  CategoryScale,
  LinearScale,
  LineController,
  PointElement,
  LineElement,
  Tooltip,
  Filler
);

export interface ChartPoint {
  label: string;
  value: number;
}

export interface ChartSeries {
  label: string;
  data: ChartPoint[];
  color: string;
}

export interface LineChartConfig {
  series?: ChartSeries[];
  data?: ChartPoint[];
  label?: string;
  currency?: string;
  stacked?: boolean;
  bare?: boolean;
}

@Component({
  selector: 'cmn-line-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (config().bare) {
      <div class="relative h-48 w-full">
        <canvas #chartCanvas></canvas>
      </div>
    } @else {
      <div
        class="flex w-full flex-col gap-cmn-3 rounded-cmn-lg border border-border-default bg-surface-card p-cmn-4"
      >
        <span
          class="font-label text-cmn-xs font-semibold uppercase tracking-wide text-text-secondary"
        >
          {{ config().label }}
        </span>
        <div class="relative h-56">
          <canvas #chartCanvas></canvas>
        </div>
      </div>
    }
  `,
})
export class LineChartComponent implements AfterViewInit, OnDestroy {
  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('chartCanvas');
  private chart: Chart | null = null;

  public readonly config = input<LineChartConfig>({});

  constructor() {
    effect(() => {
      const cfg = this.config();
      if (!this.chart) {
        return;
      }

      const seriesList = cfg.series ?? [];
      const points = cfg.data ?? [];

      if (seriesList.length > 0) {
        this.chart.data.labels = seriesList[0]?.data.map(p => p.label) ?? [];
        this.chart.data.datasets = seriesList.map((s, i) =>
          this.buildDataset(
            s.label,
            s.data.map(p => p.value),
            s.color,
            !!cfg.stacked || i === 0,
            cfg.stacked ? '4d' : '1a'
          )
        );
        if (this.chart.options.plugins?.legend) {
          this.chart.options.plugins.legend.display = !cfg.stacked;
        }
      } else if (points.length > 0) {
        this.chart.data.labels = points.map(p => p.label);
        if (this.chart.data.datasets[0]) {
          this.chart.data.datasets[0].data = points.map(p => p.value);
        }
      }

      this.chart.update('none');
    });
  }

  public ngAfterViewInit(): void {
    this.buildChart();
  }

  public ngOnDestroy(): void {
    this.chart?.destroy();
    this.chart = null;
  }

  private buildDataset(
    seriesLabel: string,
    values: number[],
    color: string,
    fill: boolean,
    fillOpacity = '1a'
  ) {
    return {
      label: seriesLabel,
      data: values,
      borderColor: color,
      backgroundColor: fill ? `${color}${fillOpacity}` : 'transparent',
      borderWidth: 2,
      pointRadius: 0,
      pointHoverRadius: 4,
      fill,
      tension: 0.3,
    };
  }

  private buildChart(): void {
    const ctx = this.canvasRef().nativeElement.getContext('2d');
    if (!ctx) {
      return;
    }

    const cfg = this.config();
    const accent = getComputedStyle(document.documentElement)
      .getPropertyValue('--color-accent-default')
      .trim();
    const textSecondary = getComputedStyle(document.documentElement)
      .getPropertyValue('--color-text-secondary')
      .trim();
    const borderDefault = getComputedStyle(document.documentElement)
      .getPropertyValue('--color-border-default')
      .trim();

    const currency = cfg.currency ?? 'USD';
    const isStacked = !!cfg.stacked;
    const seriesList = cfg.series ?? [];
    const isMulti = seriesList.length > 0;
    const points = cfg.data ?? [];

    const labels = isMulti
      ? (seriesList[0]?.data.map(p => p.label) ?? [])
      : points.map(p => p.label);
    const datasets = isMulti
      ? seriesList.map((s, i) =>
          this.buildDataset(
            s.label,
            s.data.map(p => p.value),
            s.color,
            isStacked || i === 0,
            isStacked ? '4d' : '1a'
          )
        )
      : [
          this.buildDataset(
            '',
            points.map(p => p.value),
            accent || '#4f46e5',
            true
          ),
        ];

    this.chart = new Chart(ctx, {
      type: 'line',
      data: {labels, datasets},
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: isMulti && !isStacked,
            position: 'bottom',
            labels: {
              color: textSecondary || '#464555',
              font: {family: 'Inter', size: 11},
              boxWidth: 24,
              boxHeight: 8,
              useBorderRadius: true,
              borderRadius: 2,
              padding: 12,
            },
          },
          tooltip: {
            mode: isStacked ? 'index' : 'nearest',
            intersect: !isStacked,
            callbacks: {
              label: tooltipCtx => {
                const val = tooltipCtx.parsed.y as number;
                return `${tooltipCtx.dataset.label}: ${new Intl.NumberFormat('en-US', {
                  style: 'currency',
                  currency,
                  minimumFractionDigits: 0,
                  maximumFractionDigits: 0,
                }).format(val)}`;
              },
            },
          },
        },
        scales: {
          x: {
            grid: {color: borderDefault || '#c7c4d8'},
            ticks: {color: textSecondary || '#464555', font: {family: 'Inter', size: 11}},
          },
          y: {
            stacked: isStacked,
            grid: {color: borderDefault || '#c7c4d8'},
            ticks: {
              color: textSecondary || '#464555',
              font: {family: 'Inter', size: 11},
              callback: val =>
                new Intl.NumberFormat('en-US', {
                  style: 'currency',
                  currency,
                  notation: 'compact',
                  minimumFractionDigits: 0,
                  maximumFractionDigits: 0,
                }).format(val as number),
            },
          },
        },
      },
    });
  }
}
