import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

import {IconComponent, type IconName} from '../icon/icon.component';

export type EmptyStateVariant = 'card' | 'bare';

const WRAPPER_CLASSES: Record<EmptyStateVariant, string> = {
  card: 'bg-surface-card rounded-cmn-md border border-border-default',
  bare: '',
};

const BODY_CLASSES: Record<EmptyStateVariant, string> = {
  card: 'flex flex-col items-center py-cmn-12 gap-cmn-4 text-center',
  bare: 'flex flex-col items-center py-cmn-6 gap-cmn-2 text-center',
};

const MESSAGE_CLASSES: Record<EmptyStateVariant, string> = {
  card: 'font-medium text-text-primary text-cmn-sm',
  bare: 'text-cmn-sm text-text-secondary',
};

@Component({
  selector: 'cmn-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <div [class]="wrapperClass()">
      <div [class]="bodyClass()">
        @if (icon()) {
          <cmn-icon [name]="icon()!" [class]="iconClass()" size="lg" />
        }
        <p [class]="messageClass()">{{ message() }}</p>
        @if (subMessage()) {
          <p class="text-cmn-xs text-text-secondary">{{ subMessage() }}</p>
        }
        <ng-content select="[cta]" />
      </div>
    </div>
  `,
})
export class EmptyStateComponent {
  public readonly message = input.required<string>();
  public readonly subMessage = input<string | null>(null);
  public readonly icon = input<IconName | null>(null);
  public readonly iconClass = input<string>('');
  public readonly variant = input<EmptyStateVariant>('card');

  public readonly wrapperClass = computed(() => WRAPPER_CLASSES[this.variant()]);
  public readonly bodyClass = computed(() => BODY_CLASSES[this.variant()]);
  public readonly messageClass = computed(() => MESSAGE_CLASSES[this.variant()]);
}
