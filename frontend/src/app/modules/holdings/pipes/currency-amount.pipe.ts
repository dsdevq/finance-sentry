import {DecimalPipe} from '@angular/common';
import {inject, Pipe, type PipeTransform} from '@angular/core';

const DEFAULT_CURRENCY = 'USD';

@Pipe({name: 'currencyAmount'})
export class CurrencyAmountPipe implements PipeTransform {
  private readonly decimalPipe = inject(DecimalPipe);

  public transform(value: number | null | undefined, currency?: string): string {
    const formatted = this.decimalPipe.transform(value ?? 0) ?? '';
    return `${formatted} ${currency ?? DEFAULT_CURRENCY}`;
  }
}
