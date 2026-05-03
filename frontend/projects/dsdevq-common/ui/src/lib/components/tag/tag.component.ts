import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

export type TagVariant = 'success' | 'error' | 'warning' | 'info' | 'neutral';

const VARIANT_CLASSES: Record<TagVariant, string> = {
  success: 'bg-status-success/15 text-status-success',
  error: 'bg-status-error/15 text-status-error',
  warning: 'bg-status-warning/15 text-status-warning',
  info: 'bg-status-info/15 text-status-info',
  neutral: 'bg-surface-raised text-text-secondary',
};

const BASE_CLASSES =
  'inline-flex items-center rounded-cmn-full px-cmn-2 py-0.5 ' +
  'text-cmn-xs font-label font-semibold uppercase tracking-wide';

@Component({
  selector: 'cmn-tag',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<span [class]="classes()"><ng-content /></span>',
})
export class TagComponent {
  public readonly variant = input<TagVariant>('neutral');

  public readonly classes = computed(() => `${BASE_CLASSES} ${VARIANT_CLASSES[this.variant()]}`);
}
