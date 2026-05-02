import {ChangeDetectionStrategy, Component, computed, input, output} from '@angular/core';

@Component({
  selector: 'cmn-toggle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      [attr.aria-checked]="checked()"
      [attr.aria-label]="label()"
      [class]="trackClass()"
      (click)="toggled.emit(!checked())"
      role="switch"
      type="button"
    >
      <span [class]="thumbClass()"></span>
    </button>
  `,
})
export class ToggleComponent {
  public readonly checked = input<boolean>(false);
  public readonly label = input<string>('');
  public readonly disabled = input<boolean>(false);

  public readonly toggled = output<boolean>();

  public readonly trackClass = computed(() => {
    const base =
      'relative inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full ' +
      'border-2 border-transparent transition-colors duration-200 ' +
      'focus:outline-none focus:ring-2 focus:ring-border-focus focus:ring-offset-1';
    const color = this.checked() ? 'bg-accent-default' : 'bg-border-default';
    const disabled = this.disabled() ? 'opacity-50 pointer-events-none' : '';
    return [base, color, disabled].filter(Boolean).join(' ');
  });

  public readonly thumbClass = computed(() => {
    const base =
      'pointer-events-none inline-block h-4 w-4 rounded-full bg-white shadow-sm ' +
      'transition-transform duration-200';
    const translate = this.checked() ? 'translate-x-5' : 'translate-x-0';
    return `${base} ${translate}`;
  });
}
