import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

const BASE = 'block animate-pulse rounded-cmn-sm bg-surface-raised';

@Component({
  selector: 'cmn-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<div [class]="classes()" [style.width]="width()" [style.height]="height()"></div>',
})
export class SkeletonComponent {
  public readonly width = input<string>('100%');
  public readonly height = input<string>('1.25rem');
  public readonly className = input<string>('');

  public readonly classes = computed(() => {
    const extra = this.className();
    return extra ? `${BASE} ${extra}` : BASE;
  });
}
