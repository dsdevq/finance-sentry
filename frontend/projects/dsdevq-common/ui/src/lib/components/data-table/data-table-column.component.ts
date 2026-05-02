import {ChangeDetectionStrategy, Component, contentChild, input} from '@angular/core';

import {CmnCellDirective, CmnHeaderCellDirective} from './data-table-cell.directive';

export type CmnColumnAlign = 'left' | 'right' | 'center';

@Component({
  selector: 'cmn-column',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<ng-content />',
})
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export class CmnColumnComponent<T = any> {
  public readonly key = input.required<string>();
  public readonly header = input<string>('');
  public readonly align = input<CmnColumnAlign>('left');

  public readonly cell = contentChild(CmnCellDirective<T>);
  public readonly headerCell = contentChild(CmnHeaderCellDirective);
}
