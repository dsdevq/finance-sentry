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

@Component({
  selector: 'cmn-line-chart',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex flex-col gap-cmn-3 rounded-cmn-lg border border-border-default bg-surface-card p-cmn-4"
    >
      <span
        class="font-label text-cmn-xs font-semibold uppercase tracking-wide text-text-secondary"
      >
        {{ label() }}
      </span>
      <div class="relative h-48">
        <canvas #chartCanvas></canvas>
      </div>
    </div>
  `,
})
export class LineChartComponent implements AfterViewInit, OnDestroy {
  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('chartCanvas');
  private chart: Chart | null = null;

  public readonly data = input<ChartPoint[]>([]);
  public readonly label = input<string>('');
  public readonly currency = input<string>('USD');

  constructor() {
    effect(() => {
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
    const points = this.data();

    this.chart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: points.map(p => p.label),
        datasets: [
          {
            data: points.map(p => p.value),
            borderColor: accent || '#4f46e5',
            backgroundColor: `${accent || '#4f46e5'}1a`,
            borderWidth: 2,
            pointRadius: 0,
            pointHoverRadius: 4,
            fill: true,
            tension: 0.3,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {display: false},
          tooltip: {
            callbacks: {
              label: ctx => {
                const val = ctx.parsed.y as number;
                return new Intl.NumberFormat('en-US', {
                  style: 'currency',
                  currency,
                  minimumFractionDigits: 0,
                  maximumFractionDigits: 0,
                }).format(val);
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
