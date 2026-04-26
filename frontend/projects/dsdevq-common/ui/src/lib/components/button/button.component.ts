import {ChangeDetectionStrategy, Component, computed, input, output} from '@angular/core';

import {type LucideIconName, IconComponent} from '../icon/icon.component';

export type ButtonVariant = 'primary' | 'secondary' | 'destructive';
export type ButtonSize = 'sm' | 'md' | 'lg';
export type ButtonType = 'button' | 'submit' | 'reset';
export type ButtonIconPosition = 'prefix' | 'suffix';

const BASE_CLASSES =
  'inline-flex items-center justify-center gap-cmn-2 font-label font-medium rounded-cmn-md ' +
  'focus:outline-none focus:ring-2 focus:ring-border-focus focus:ring-offset-1 ' +
  'transition-colors duration-150';

const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  primary: 'bg-accent-default text-text-inverse hover:bg-accent-hover active:bg-accent-active',
  secondary:
    'bg-transparent border border-border-default text-text-primary hover:bg-surface-raised active:bg-surface-raised',
  destructive: 'bg-status-error text-text-inverse hover:opacity-90 active:opacity-80',
};

const SIZE_CLASSES: Record<ButtonSize, string> = {
  sm: 'px-cmn-2 py-cmn-1 text-cmn-sm',
  md: 'px-cmn-4 py-cmn-2 text-cmn-md',
  lg: 'px-cmn-6 py-cmn-3 text-cmn-lg',
};

const ICON_SIZE: Record<ButtonSize, 'sm' | 'md'> = {
  sm: 'sm',
  md: 'sm',
  lg: 'md',
};

const DISABLED_CLASSES = 'opacity-50 pointer-events-none cursor-not-allowed';

@Component({
  selector: 'cmn-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    <button
      [type]="type()"
      [disabled]="disabled() || loading()"
      [attr.aria-busy]="loading() ? true : null"
      [attr.aria-disabled]="disabled() ? true : null"
      [class]="classes()"
      (click)="clicked.emit($event)"
    >
      @if (loading()) {
        <span class="animate-spin inline-flex">
          <cmn-icon name="LoaderCircle" [size]="iconSize()" />
        </span>
      } @else if (icon() && iconPosition() === 'prefix') {
        <cmn-icon [name]="icon()!" [size]="iconSize()" aria-hidden="true" />
      }
      <ng-content />
      @if (!loading() && icon() && iconPosition() === 'suffix') {
        <cmn-icon [name]="icon()!" [size]="iconSize()" aria-hidden="true" />
      }
    </button>
  `,
})
export class ButtonComponent {
  public readonly variant = input<ButtonVariant>('primary');
  public readonly size = input<ButtonSize>('md');
  public readonly type = input<ButtonType>('button');
  public readonly disabled = input<boolean>(false);
  public readonly loading = input<boolean>(false);
  public readonly icon = input<Nullable<LucideIconName>>(null);
  public readonly iconPosition = input<ButtonIconPosition>('prefix');

  public readonly clicked = output<MouseEvent>();

  public readonly classes = computed(() => {
    const parts = [BASE_CLASSES, VARIANT_CLASSES[this.variant()], SIZE_CLASSES[this.size()]];
    if (this.disabled() || this.loading()) {
      parts.push(DISABLED_CLASSES);
    }
    return parts.join(' ');
  });

  public readonly iconSize = computed(() => ICON_SIZE[this.size()]);
}
