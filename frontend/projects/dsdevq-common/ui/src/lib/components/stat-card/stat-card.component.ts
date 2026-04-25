import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

import {IconComponent, LucideIconName} from '../icon/icon.component';

const POSITIVE_DELTA_CLASSES = 'text-status-success';
const NEGATIVE_DELTA_CLASSES = 'text-status-error';
const NEUTRAL_DELTA_CLASSES = 'text-text-secondary';

@Component({
  selector: 'cmn-stat-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <div class="flex flex-col gap-cmn-2 rounded-cmn-lg border border-border-default bg-surface-card p-cmn-4">
      <div class="flex items-center justify-between">
        <span class="font-label text-cmn-xs font-semibold uppercase tracking-wide text-text-secondary">
          {{ label() }}
        </span>
        @if (icon()) {
          <cmn-icon [name]="icon()!" size="sm" class="text-text-secondary" />
        }
      </div>

      @if (loading()) {
        <div class="h-8 w-3/4 animate-pulse rounded-cmn-sm bg-surface-raised"></div>
        <div class="h-4 w-1/2 animate-pulse rounded-cmn-sm bg-surface-raised"></div>
      } @else {
        <span class="font-mono text-cmn-2xl font-semibold tabular-nums text-text-primary">
          {{ value() }}
        </span>
        @if (delta() !== null) {
          <div class="flex items-center gap-1">
            <cmn-icon
              [name]="delta()! >= 0 ? 'TrendingUp' : 'TrendingDown'"
              size="sm"
              [class]="deltaClasses()"
            />
            <span class="font-label text-cmn-xs font-medium" [class]="deltaClasses()">
              {{ deltaLabel() }}
            </span>
          </div>
        }
      }
    </div>
  `,
})
export class StatCardComponent {
  public readonly label = input.required<string>();
  public readonly value = input<string>('—');
  public readonly delta = input<number | null>(null);
  public readonly deltaLabel = input<string>('');
  public readonly icon = input<LucideIconName | null>(null);
  public readonly loading = input<boolean>(false);

  public readonly deltaClasses = computed(() => {
    const d = this.delta();
    if (d === null) return NEUTRAL_DELTA_CLASSES;
    return d >= 0 ? POSITIVE_DELTA_CLASSES : NEGATIVE_DELTA_CLASSES;
  });
}
