import {Directive, inject, TemplateRef} from '@angular/core';

export interface CmnCellContext<T> {
  $implicit: T;
  index: number;
}

@Directive({selector: '[cmnCell]'})
// Default `any` keeps `let-row` usable without an explicit column type in consumer
// templates (same pattern Angular uses for structural-directive context typing).
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export class CmnCellDirective<T = any> {
  public readonly template = inject<TemplateRef<CmnCellContext<T>>>(TemplateRef);

  public static ngTemplateContextGuard<T>(
    _dir: CmnCellDirective<T>,
    ctx: unknown
  ): ctx is CmnCellContext<T> {
    return true;
  }
}

@Directive({selector: '[cmnHeaderCell]'})
export class CmnHeaderCellDirective {
  public readonly template = inject<TemplateRef<unknown>>(TemplateRef);
}
