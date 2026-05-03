import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';
import {type ChartSeries, LineChartComponent, type LineChartConfig} from '@dsdevq-common/ui';

const PERCENT = 100;
const DECIMAL_PLACES = 1;

@Component({
  selector: 'fns-net-worth-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LineChartComponent],
  template: `
    <div class="flex w-full flex-col rounded-cmn-lg border border-border-default bg-surface-card">
      <div class="flex items-start justify-between p-cmn-4 pb-cmn-2">
        <div class="flex flex-col gap-cmn-1">
          <span
            class="font-label text-cmn-xs font-semibold uppercase tracking-wide text-text-secondary"
          >
            Net Worth Over Time
          </span>
          @if (series().length > 0) {
            <div class="flex items-baseline gap-cmn-2">
              <span class="font-mono text-2xl font-bold tracking-tight text-text-primary">
                {{ currentTotal() }}
              </span>
              <span
                [class]="isPositive() ? 'text-status-success' : 'text-status-error'"
                class="text-cmn-sm font-medium"
              >
                {{ delta() }}
              </span>
            </div>
          }
        </div>
        @if (series().length > 0) {
          <div class="flex items-center gap-cmn-4">
            @for (s of series(); track s.label) {
              <div class="flex items-center gap-cmn-1">
                <span
                  [style.background-color]="s.color"
                  class="inline-block h-2.5 w-2.5 rounded-sm"
                ></span>
                <span class="text-cmn-xs text-text-secondary">{{ s.label }}</span>
              </div>
            }
          </div>
        }
      </div>
      <cmn-line-chart [config]="chartConfig()" />
    </div>
  `,
})
export class NetWorthChartComponent {
  private readonly endVal = computed(() => {
    const s = this.series();
    if (s.length === 0 || s[0].data.length === 0) {
      return 0;
    }
    const lastIdx = s[0].data.length - 1;
    return s.reduce((sum, ser) => sum + (ser.data[lastIdx]?.value ?? 0), 0);
  });

  private readonly startVal = computed(() => {
    const s = this.series();
    if (s.length === 0 || s[0].data.length === 0) {
      return 0;
    }
    return s.reduce((sum, ser) => sum + (ser.data[0]?.value ?? 0), 0);
  });

  public readonly series = input<ChartSeries[]>([]);
  public readonly currency = input<string>('USD');

  public readonly chartConfig = computed(
    (): LineChartConfig => ({
      series: this.series(),
      currency: this.currency(),
      stacked: true,
      bare: true,
    })
  );

  public readonly currentTotal = computed(() =>
    new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: this.currency(),
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(this.endVal())
  );

  public readonly delta = computed(() => {
    const change = this.endVal() - this.startVal();
    const absPct = this.startVal() > 0 ? Math.abs((change / this.startVal()) * PERCENT) : 0;
    const sign = change >= 0 ? '+' : '-';
    const absChange = new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: this.currency(),
      notation: 'compact',
      minimumFractionDigits: 0,
      maximumFractionDigits: DECIMAL_PLACES,
    }).format(Math.abs(change));
    return `${sign}${absChange} (${sign}${absPct.toFixed(DECIMAL_PLACES)}%)`;
  });

  public readonly isPositive = computed(() => this.endVal() >= this.startVal());
}
