import {ChangeDetectionStrategy, Component, input} from '@angular/core';

export type SectionHeaderAlign = 'between' | 'start';
export type SectionHeaderLabelAs = 'h2' | 'span';

const LABEL_CLASSES =
  'font-label text-cmn-xs font-semibold uppercase tracking-wide text-text-secondary';

@Component({
  selector: 'cmn-section-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'flex items-center',
    '[class.justify-between]': 'align() === "between"',
    '[class.gap-cmn-2]': 'align() === "start"',
  },
  template: `
    @if (labelAs() === 'h2') {
      <h2 class="${LABEL_CLASSES}">{{ label() }}</h2>
    } @else {
      <span class="${LABEL_CLASSES}">{{ label() }}</span>
    }
    <ng-content />
  `,
})
export class SectionHeaderComponent {
  public readonly label = input.required<string>();
  public readonly align = input<SectionHeaderAlign>('between');
  public readonly labelAs = input<SectionHeaderLabelAs>('h2');
}
