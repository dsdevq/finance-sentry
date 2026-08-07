import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

export type CardPadding = 'none' | 'sm' | 'md' | 'lg';

const PADDING_CLASSES: Record<CardPadding, string> = {
  none: '',
  sm: 'p-cmn-2',
  md: 'p-cmn-4',
  lg: 'p-cmn-6',
};

@Component({
  selector: 'cmn-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.flex]': 'fill()',
    '[class.flex-col]': 'fill()',
    '[class.min-h-0]': 'fill()',
  },
  template: `
    <div [class]="classes()">
      <ng-content />
    </div>
  `,
})
export class CardComponent {
  public readonly padding = input<CardPadding>('md');
  public readonly elevated = input<boolean>(false);
  /**
   * Fill mode: the card stretches to its flex parent and clips overflow, so a
   * child marked `flex-1 min-h-0 overflow-auto` scrolls internally (with a
   * sticky header) instead of growing the page. The host must be sized by its
   * parent (e.g. `class="flex-1 min-h-0"`).
   */
  public readonly fill = input<boolean>(false);

  public readonly classes = computed(() => {
    const parts: string[] = [
      'bg-surface-card',
      'rounded-cmn-md',
      'border',
      'border-border-default',
    ];
    const paddingClass = PADDING_CLASSES[this.padding()];
    if (paddingClass) {
      parts.push(paddingClass);
    }
    if (this.elevated()) {
      parts.push('shadow-cmn-md');
    }
    if (this.fill()) {
      parts.push('flex', 'min-h-0', 'flex-1', 'flex-col', 'overflow-hidden');
    }
    return parts.join(' ');
  });
}
