import {ChangeDetectionStrategy, Component, computed, input, output} from '@angular/core';

export type SelectableCardOrientation = 'horizontal' | 'vertical';

const BASE_CLASSES =
  'group w-full h-full flex rounded-cmn-md border bg-surface-card text-left ' +
  'transition-colors focus:outline-none focus:ring-2 focus:ring-border-focus ' +
  'disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:border-border-default';

const ORIENTATION_CLASSES: Record<SelectableCardOrientation, string> = {
  horizontal: 'flex-row items-center gap-cmn-3 p-cmn-4',
  vertical: 'flex-col items-center text-center gap-cmn-2 p-cmn-5',
};

const STATE_CLASSES = {
  default: 'border-border-default hover:border-accent-default hover:bg-surface-raised',
  selected: 'border-accent-default bg-accent-subtle',
};

@Component({
  selector: 'cmn-selectable-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      [class]="classes()"
      [disabled]="disabled()"
      [attr.aria-pressed]="selected() ? true : null"
      (click)="clicked.emit()"
    >
      <span class="cmn-selectable-card__leading flex-shrink-0">
        <ng-content select="[leading]" />
      </span>
      <span [class]="bodyClasses()">
        <ng-content />
      </span>
      <span class="cmn-selectable-card__trailing flex-shrink-0 ml-auto">
        <ng-content select="[trailing]" />
      </span>
    </button>
  `,
})
export class SelectableCardComponent {
  public readonly orientation = input<SelectableCardOrientation>('horizontal');
  public readonly selected = input<boolean>(false);
  public readonly disabled = input<boolean>(false);

  public readonly clicked = output<void>();

  public readonly classes = computed(() => {
    const state = this.selected() ? STATE_CLASSES.selected : STATE_CLASSES.default;
    return [BASE_CLASSES, ORIENTATION_CLASSES[this.orientation()], state].join(' ');
  });

  public readonly bodyClasses = computed(() =>
    this.orientation() === 'horizontal'
      ? 'flex-1 flex flex-col gap-cmn-1 min-w-0'
      : 'flex flex-col gap-cmn-1'
  );
}
