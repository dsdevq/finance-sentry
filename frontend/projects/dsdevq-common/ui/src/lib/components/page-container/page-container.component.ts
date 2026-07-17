import {ChangeDetectionStrategy, Component, computed, input} from '@angular/core';

export type PageContainerSpacing = 'md' | 'lg';

const SPACING_CLASSES: Record<PageContainerSpacing, string> = {
  md: 'space-y-cmn-5',
  lg: 'space-y-cmn-10',
};

@Component({
  selector: 'cmn-page-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {style: 'display: block'},
  template: `
    <div class="p-cmn-6">
      <div [class]="innerClasses()">
        <ng-content />
      </div>
    </div>
  `,
})
export class PageContainerComponent {
  public readonly maxWidth = input<string>('max-w-screen-lg');
  public readonly spacing = input<PageContainerSpacing>('md');

  public readonly innerClasses = computed(
    () => `mx-auto ${this.maxWidth()} ${SPACING_CLASSES[this.spacing()]}`
  );
}
