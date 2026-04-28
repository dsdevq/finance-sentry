import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

export type StatusIndicatorVariant = 'success' | 'warning' | 'error' | 'neutral';

const VARIANT_TEXT: Record<StatusIndicatorVariant, string> = {
  success: 'text-status-success',
  warning: 'text-status-warning',
  error: 'text-status-error',
  neutral: 'text-text-secondary',
};

const VARIANT_DOT: Record<StatusIndicatorVariant, string> = {
  success: 'bg-status-success',
  warning: 'bg-status-warning',
  error: 'bg-status-error',
  neutral: 'bg-text-secondary',
};

const STATUS_BASE =
  'inline-flex items-center gap-1.5 font-bold text-[10px] uppercase tracking-wide';

@Component({
  selector: 'cmn-status-indicator',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col gap-0.5">
      <span [class]="statusClasses()">
        <span [class]="dotClasses()"></span>
        <ng-content />
      </span>
      @if (timestampLabel()) {
        <span class="text-[10px] text-text-secondary font-medium">{{ timestampLabel() }}</span>
      }
    </div>
  `,
})
export class StatusIndicatorComponent {
  public readonly variant = input<StatusIndicatorVariant>('neutral');
  public readonly timestampLabel = input<string | null>(null);

  public readonly statusClasses = computed(() => `${STATUS_BASE} ${VARIANT_TEXT[this.variant()]}`);

  public readonly dotClasses = computed(
    () => `w-1 h-1 rounded-full ${VARIANT_DOT[this.variant()]}`
  );
}
