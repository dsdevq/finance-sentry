import {CurrencyPipe} from '@angular/common';
import {Pipe, type PipeTransform} from '@angular/core';

const DEFAULT_CURRENCY = 'USD';
const DEFAULT_DISPLAY = 'symbol';
const DEFAULT_DIGITS_INFO = '1.2-2';

@Pipe({name: 'appCurrency'})
export class AppCurrencyPipe extends CurrencyPipe implements PipeTransform {
  public override transform(
    value: number | string,
    currencyCode?: string,
    display?: 'code' | 'symbol' | 'symbol-narrow' | string | boolean,
    digitsInfo?: string,
    locale?: string
  ): Nullable<string>;
  public override transform(
    value: null | undefined,
    currencyCode?: string,
    display?: 'code' | 'symbol' | 'symbol-narrow' | string | boolean,
    digitsInfo?: string,
    locale?: string
  ): null;
  public override transform(
    value: number | string | null | undefined,
    currencyCode?: string,
    display?: 'code' | 'symbol' | 'symbol-narrow' | string | boolean,
    digitsInfo?: string,
    locale?: string
  ): Nullable<string>;
  public override transform(
    value: number | string | null | undefined,
    currencyCode?: string,
    display?: 'code' | 'symbol' | 'symbol-narrow' | string | boolean,
    digitsInfo?: string,
    locale?: string
  ): Nullable<string> {
    return super.transform(
      value as number,
      currencyCode ?? DEFAULT_CURRENCY,
      display ?? DEFAULT_DISPLAY,
      digitsInfo ?? DEFAULT_DIGITS_INFO,
      locale
    );
  }
}
