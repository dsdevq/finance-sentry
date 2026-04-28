import {ChangeDetectionStrategy, Component, computed, input, output} from '@angular/core';

import {type LucideIconName, IconComponent} from '../icon/icon.component';

export type AlertVariant = 'info' | 'success' | 'warning' | 'error' | 'accent';

const VARIANT_CONTAINER_CLASSES: Record<AlertVariant, string> = {
  info: 'bg-status-info/10    border border-status-info    text-text-primary',
  success: 'bg-status-success/10 border border-status-success text-text-primary',
  warning: 'bg-status-warning/10 border border-status-warning text-text-primary',
  error: 'bg-status-error/10   border border-status-error   text-text-primary',
  accent: 'bg-accent-subtle    border border-accent-default/20 text-accent-default',
};

const VARIANT_ICON_COLOR: Record<AlertVariant, string> = {
  info: 'var(--color-status-info)',
  success: 'var(--color-status-success)',
  warning: 'var(--color-status-warning)',
  error: 'var(--color-status-error)',
  accent: 'var(--color-accent-default)',
};

const VARIANT_ICON_NAME: Record<AlertVariant, LucideIconName> = {
  info: 'Info',
  success: 'CircleCheck',
  warning: 'TriangleAlert',
  error: 'CircleAlert',
  accent: 'Sparkles',
};

const VARIANT_ROLE: Record<AlertVariant, string> = {
  info: 'status',
  success: 'status',
  warning: 'alert',
  error: 'alert',
  accent: 'note',
};

@Component({
  selector: 'cmn-alert',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <div [attr.role]="role()" [class]="containerClasses()">
      <cmn-icon [name]="resolvedIcon()" [color]="iconColor()" size="sm" aria-hidden="true" />

      <div class="flex-1 min-w-0">
        @if (title()) {
          <p class="font-label text-cmn-sm font-semibold mb-0.5">{{ title() }}</p>
        }
        <ng-content />
      </div>

      @if (dismissible()) {
        <button
          (click)="dismissed.emit()"
          type="button"
          class="ml-cmn-2 -mt-0.5 -mr-0.5 inline-flex items-center justify-center rounded-cmn-sm p-0.5 hover:bg-black/10 focus:outline-none focus:ring-2 focus:ring-border-focus"
          aria-label="Dismiss"
        >
          <cmn-icon name="X" size="sm" aria-hidden="true" />
        </button>
      }
    </div>
  `,
})
export class AlertComponent {
  public readonly variant = input<AlertVariant>('info');
  public readonly title = input<string>('');
  public readonly icon = input<Nullable<LucideIconName>>(null);
  public readonly dismissible = input<boolean>(false);

  public readonly dismissed = output<void>();

  public readonly role = computed(() => VARIANT_ROLE[this.variant()]);
  public readonly resolvedIcon = computed<LucideIconName>(
    () => this.icon() ?? VARIANT_ICON_NAME[this.variant()]
  );
  public readonly iconColor = computed(() => VARIANT_ICON_COLOR[this.variant()]);

  public readonly containerClasses = computed(() =>
    [
      'flex items-start gap-cmn-2 rounded-cmn-md p-cmn-3',
      VARIANT_CONTAINER_CLASSES[this.variant()],
    ].join(' ')
  );
}
