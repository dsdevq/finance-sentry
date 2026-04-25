import {DecimalPipe} from '@angular/common';
import {inject, Pipe, type PipeTransform} from '@angular/core';

import {HoldingsStore} from '../store/holdings.store';

@Pipe({name: 'currencyAmount'})
export class CurrencyAmountPipe implements PipeTransform {
  private readonly decimalPipe = inject(DecimalPipe);
  private readonly store = inject(HoldingsStore);

  public transform(value: number | null | undefined, currency?: string): string {
    const formatted = this.decimalPipe.transform(value ?? 0) ?? '';
    return `${formatted} ${currency ?? this.store.baseCurrency()}`;
  }
}
