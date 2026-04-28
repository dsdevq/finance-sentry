import {ChangeDetectionStrategy, Component, computed, inject, input} from '@angular/core';
import {takeUntilDestroyed, toObservable, toSignal} from '@angular/core/rxjs-interop';
import {type SafeHtml} from '@angular/platform-browser';
import {of, switchMap} from 'rxjs';
import {icons, LUCIDE_ICONS, LucideAngularModule, LucideIconProvider} from 'lucide-angular';

import {CmnIconRegistry} from '../../services/icon-registry/icon-registry.service';

export type IconSize = 'sm' | 'md' | 'lg';
export type LucideIconName = keyof typeof icons;
export type IconName = LucideIconName | (string & {});

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
    @if (customSvg(); as svg) {
      <span
        [innerHTML]="svg"
        [style.display]="'inline-flex'"
        [style.width.px]="resolvedSize()"
        [style.height.px]="resolvedSize()"
        [style.color]="color()"
        [attr.aria-hidden]="ariaLabel() ? null : 'true'"
        [attr.aria-label]="ariaLabel() || null"
        class="cmn-icon-custom"
      ></span>
    } @else if (isLucide()) {
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
  private readonly registry = inject(CmnIconRegistry);

  public readonly name = input.required<IconName>();
  public readonly size = input<IconSize>('md');
  public readonly color = input<string>('currentColor');
  public readonly ariaLabel = input<string>('');

  public readonly resolvedSize = computed(() => SIZE_PX[this.size()]);
  public readonly isLucide = computed(() =>
    Object.prototype.hasOwnProperty.call(icons, this.name())
  );

  protected readonly customSvg = toSignal<SafeHtml | null>(
    toObservable(this.name).pipe(
      switchMap(n => (this.registry.has(n) ? this.registry.resolve(n) : of(null))),
      takeUntilDestroyed()
    ),
    {initialValue: null}
  );
}
