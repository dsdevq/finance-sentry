import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

export type DialogActionsAlign = 'between' | 'end' | 'start';

const BASE_CLASSES =
  '-mx-cmn-6 -mb-cmn-6 mt-cmn-6 flex items-center gap-cmn-2 ' +
  'border-t border-border-default px-cmn-6 py-cmn-4';

const ALIGN_CLASSES: Record<DialogActionsAlign, string> = {
  between: 'justify-between',
  end: 'justify-end',
  start: 'justify-start',
};

@Component({
  selector: 'cmn-dialog-actions',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [class]="classes()">
      <ng-content />
    </div>
  `,
})
export class DialogActionsComponent {
  public readonly align = input<DialogActionsAlign>('end');

  public readonly classes = computed(() => `${BASE_CLASSES} ${ALIGN_CLASSES[this.align()]}`);
}
