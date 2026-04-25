import {DecimalPipe} from '@angular/common';
import {inject, Pipe, type PipeTransform} from '@angular/core';

import {type AccountBalanceItem} from '../../../shared/models/wealth/wealth.model';

@Pipe({name: 'holdingBalance'})
export class HoldingBalancePipe implements PipeTransform {
  private readonly decimalPipe = inject(DecimalPipe);

  public transform({balanceInBaseCurrency, currentBalance}: AccountBalanceItem): string {
    const value = balanceInBaseCurrency ?? currentBalance;
    return this.decimalPipe.transform(value) ?? '';
  }
}
