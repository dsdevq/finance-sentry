import {Directive, effect, ElementRef, inject, input, Renderer2} from '@angular/core';

export type TypographyLevel =
  | 'display'
  | 'h1'
  | 'h2'
  | 'h3'
  | 'h4'
  | 'body'
  | 'small'
  | 'caption'
  | 'label'
  | 'code';

const LEVEL_CLASSES: Record<TypographyLevel, string[]> = {
  display: ['font-headline', 'text-cmn-4xl', 'font-bold', 'leading-tight', 'tracking-tight'],
  h1: ['font-headline', 'text-cmn-3xl', 'font-bold'],
  h2: ['font-headline', 'text-cmn-2xl', 'font-semibold'],
  h3: ['font-headline', 'text-cmn-xl', 'font-semibold'],
  h4: ['font-headline', 'text-cmn-lg', 'font-medium'],
  body: ['font-base', 'text-cmn-md', 'font-normal'],
  small: ['font-base', 'text-cmn-sm', 'font-normal'],
  caption: ['font-base', 'text-cmn-xs', 'font-normal', 'text-text-secondary'],
  label: ['font-label', 'text-cmn-sm', 'font-medium'],
  code: ['font-mono', 'text-cmn-sm'],
};

@Directive({
  selector: '[cmnTypography]',
})
export class TypographyDirective {
  private readonly el = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private previousClasses: string[] = [];

  public readonly cmnTypography = input<TypographyLevel>('body');

  constructor() {
    effect(() => {
      const classes = LEVEL_CLASSES[this.cmnTypography()];
      for (const cls of this.previousClasses) {
        this.renderer.removeClass(this.el.nativeElement, cls);
      }
      for (const cls of classes) {
        this.renderer.addClass(this.el.nativeElement, cls);
      }
      this.previousClasses = classes;
    });
  }
}
