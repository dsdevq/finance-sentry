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
import {ArcElement, Chart, DoughnutController, Legend, Tooltip} from 'chart.js';

Chart.register(DoughnutController, ArcElement, Tooltip, Legend);

export interface DonutSegment {
  label: string;
  value: number;
  color?: string;
}

const DEFAULT_COLORS = [
  '#4f46e5',
  '#10b981',
  '#f59e0b',
  '#ef4444',
  '#6366f1',
  '#34d399',
  '#fbbf24',
  '#a78bfa',
];

@Component({
  selector: 'cmn-donut-chart',
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
      <div class="relative w-full" style="min-height: 280px">
        <canvas #chartCanvas></canvas>
      </div>
    </div>
  `,
})
export class DonutChartComponent implements AfterViewInit, OnDestroy {
  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('chartCanvas');
  private chart: Chart | null = null;

  public readonly segments = input<DonutSegment[]>([]);
  public readonly label = input<string>('');
  public readonly currency = input<string>('USD');

  constructor() {
    effect(() => {
      const segs = this.segments();
      if (this.chart) {
        this.chart.data.labels = segs.map(s => s.label);
        this.chart.data.datasets[0].data = segs.map(s => s.value);
        this.chart.data.datasets[0].backgroundColor = segs.map(
          (s, i) => s.color ?? DEFAULT_COLORS[i % DEFAULT_COLORS.length]
        );
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

    const textSecondary = getComputedStyle(document.documentElement)
      .getPropertyValue('--color-text-secondary')
      .trim();
    const segs = this.segments();
    const currency = this.currency();

    this.chart = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: segs.map(s => s.label),
        datasets: [
          {
            data: segs.map(s => s.value),
            backgroundColor: segs.map(
              (s, i) => s.color ?? DEFAULT_COLORS[i % DEFAULT_COLORS.length]
            ),
            borderWidth: 0,
            hoverOffset: 4,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '70%',
        plugins: {
          legend: {
            position: 'bottom',
            labels: {
              color: textSecondary || '#464555',
              font: {family: 'Inter', size: 11},
              boxWidth: 10,
              padding: 12,
            },
          },
          tooltip: {
            callbacks: {
              label: ctx => {
                const val = ctx.parsed;
                const formatted = new Intl.NumberFormat('en-US', {
                  style: 'currency',
                  currency,
                  minimumFractionDigits: 0,
                  maximumFractionDigits: 0,
                }).format(val);
                const total = ctx.dataset.data.reduce((a, b) => a + b, 0);
                const pct = total > 0 ? ((val / total) * 100).toFixed(1) : '0.0';
                return `${formatted} (${pct}%)`;
              },
            },
          },
        },
      },
    });
  }
}
