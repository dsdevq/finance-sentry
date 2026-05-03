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

Chart.register(CategoryScale, LinearScale, LineController, PointElement, LineElement, Tooltip, Filler);

export interface ChartPoint {
  label: string;
  value: number;
}

export interface ChartSeries {
  label: string;
  data: ChartPoint[];
  color: string;
}

@Component({
  selector: 'cmn-line-chart',
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
      <div class="relative h-56">
        <canvas #chartCanvas></canvas>
      </div>
    </div>
  `,
})
export class LineChartComponent implements AfterViewInit, OnDestroy {
  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('chartCanvas');
  private chart: Chart | null = null;

  public readonly data = input<ChartPoint[]>([]);
  public readonly series = input<ChartSeries[]>([]);
  public readonly label = input<string>('');
  public readonly currency = input<string>('USD');

  constructor() {
    effect(() => {
      const seriesList = this.series();
      if (seriesList.length > 0) {
        if (this.chart) {
          const labels = seriesList[0]?.data.map(p => p.label) ?? [];
          this.chart.data.labels = labels;
          this.chart.data.datasets = seriesList.map((s, i) =>
            this.buildDataset(s.label, s.data.map(p => p.value), s.color, i === 0)
          );
          if (this.chart.options.plugins?.legend) {
            this.chart.options.plugins.legend.display = true;
          }
          this.chart.update('none');
        }
        return;
      }
      const points = this.data();
      if (this.chart) {
        this.chart.data.labels = points.map(p => p.label);
        this.chart.data.datasets[0].data = points.map(p => p.value);
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

  private buildDataset(seriesLabel: string, values: number[], color: string, fill: boolean) {
    return {
      label: seriesLabel,
      data: values,
      borderColor: color,
      backgroundColor: fill ? `${color}1a` : 'transparent',
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

    const accent = getComputedStyle(document.documentElement)
      .getPropertyValue('--color-accent-default')
      .trim();
    const textSecondary = getComputedStyle(document.documentElement)
      .getPropertyValue('--color-text-secondary')
      .trim();
    const borderDefault = getComputedStyle(document.documentElement)
      .getPropertyValue('--color-border-default')
      .trim();

    const currency = this.currency();
    const seriesList = this.series();
    const isMulti = seriesList.length > 0;
    const points = this.data();

    const labels = isMulti
      ? (seriesList[0]?.data.map(p => p.label) ?? [])
      : points.map(p => p.label);

    const datasets = isMulti
      ? seriesList.map((s, i) =>
          this.buildDataset(s.label, s.data.map(p => p.value), s.color, i === 0)
        )
      : [this.buildDataset('', points.map(p => p.value), accent || '#4f46e5', true)];

    this.chart = new Chart(ctx, {
      type: 'line',
      data: {labels, datasets},
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: isMulti,
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
            callbacks: {
              label: ctx => {
                const val = ctx.parsed.y as number;
                return `${ctx.dataset.label}: ${new Intl.NumberFormat('en-US', {
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
