import {DecimalPipe} from '@angular/common';
import {inject, Pipe, type PipeTransform} from '@angular/core';

import {type AccountBalanceItem} from '../../../shared/models/wealth/wealth.model';

@Pipe({name: 'accountBalance'})
export class AccountBalancePipe implements PipeTransform {
  private readonly decimalPipe = inject(DecimalPipe);

  public transform({balanceInBaseCurrency, currentBalance, currency}: AccountBalanceItem): string {
    const value = balanceInBaseCurrency ?? currentBalance;
    return `${currency} ${this.decimalPipe.transform(value) ?? ''}`;
  }
}
