import {ChangeDetectionStrategy, Component, computed, input, output} from '@angular/core';

const BASE_CLASSES =
  'inline-flex items-center rounded-cmn-full px-cmn-3 py-cmn-1 text-cmn-sm ' +
  'font-medium cursor-pointer select-none transition-colors focus:outline-none ' +
  'focus:ring-2 focus:ring-border-focus focus:ring-offset-1';

const SELECTED_CLASSES = 'bg-accent-default text-text-inverse';
const UNSELECTED_CLASSES = 'bg-surface-raised text-text-secondary hover:bg-surface-hover';

@Component({
  selector: 'cmn-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      [class]="classes()"
      [attr.aria-pressed]="selected()"
      (click)="clicked.emit()"
      type="button"
    >
      <ng-content />
    </button>
  `,
})
export class ChipComponent {
  public readonly selected = input<boolean>(false);

  public readonly clicked = output<void>();

  public readonly classes = computed(
    () => `${BASE_CLASSES} ${this.selected() ? SELECTED_CLASSES : UNSELECTED_CLASSES}`
  );
}
