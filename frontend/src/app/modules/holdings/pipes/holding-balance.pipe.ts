import {DecimalPipe} from '@angular/common';
import {inject, Pipe, type PipeTransform} from '@angular/core';

import {type AccountBalanceItem} from '../../../shared/models/wealth/wealth.model';
import {type FormattedBalance} from '../../bank-sync/pipes/account-balance.pipe';

@Pipe({name: 'holdingBalance'})
export class HoldingBalancePipe implements PipeTransform {
  private readonly decimalPipe = inject(DecimalPipe);

  public transform({
    currentBalance,
    currency,
    balanceInBaseCurrency,
  }: AccountBalanceItem): FormattedBalance {
    const nativeAmount = this.decimalPipe.transform(currentBalance, '1.2-2') ?? '';
    const native = `${nativeAmount} ${currency}`;

    if (
      currency === 'USD' ||
      balanceInBaseCurrency === null ||
      balanceInBaseCurrency === currentBalance
    ) {
      return {native, usd: null};
    }

    const usdAmount = this.decimalPipe.transform(balanceInBaseCurrency, '1.0-0') ?? '';
    return {native, usd: `~ $${usdAmount}`};
  }
}
