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
  template: `
    <div [class]="classes()">
      <ng-content />
    </div>
  `,
})
export class CardComponent {
  public readonly padding = input<CardPadding>('md');
  public readonly elevated = input<boolean>(false);

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
    return parts.join(' ');
  });
}
