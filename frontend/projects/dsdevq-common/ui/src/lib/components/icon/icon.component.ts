import {ChangeDetectionStrategy, Component, computed, effect, input} from '@angular/core';
import {icons, LUCIDE_ICONS, LucideAngularModule, LucideIconProvider} from 'lucide-angular';

export type IconSize = 'sm' | 'md' | 'lg';
export type LucideIconName = keyof typeof icons;

const SIZE_PX: Record<IconSize, number> = {
  sm: 16,
  md: 20,
  lg: 24,
};

@Component({
  selector: 'cmn-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideAngularModule],
  providers: [
    {
      provide: LUCIDE_ICONS,
      multi: true,
      useValue: new LucideIconProvider(icons),
    },
  ],
  template: `
    @if (isKnown()) {
      <lucide-icon
        [name]="name()"
        [size]="resolvedSize()"
        [color]="color()"
        [attr.aria-hidden]="ariaLabel() ? null : 'true'"
        [attr.aria-label]="ariaLabel() || null"
      />
    } @else {
      <span
        [style.display]="'inline-block'"
        [style.width.px]="resolvedSize()"
        [style.height.px]="resolvedSize()"
        [attr.aria-hidden]="ariaLabel() ? null : 'true'"
        [attr.aria-label]="ariaLabel() || null"
      ></span>
    }
  `,
})
export class IconComponent {
  public readonly name = input.required<LucideIconName>();
  public readonly size = input<IconSize>('md');
  public readonly color = input<string>('currentColor');
  public readonly ariaLabel = input<string>('');

  public readonly resolvedSize = computed(() => SIZE_PX[this.size()]);

  public readonly isKnown = computed(() =>
    Object.prototype.hasOwnProperty.call(icons, this.name())
  );

  constructor() {
    effect(() => {
      if (!this.isKnown()) {
        console.warn(`[cmn-icon] Unknown icon name: "${this.name()}"`);
      }
    });
  }
}
