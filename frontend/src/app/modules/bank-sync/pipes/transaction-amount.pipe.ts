import {DecimalPipe} from '@angular/common';
import {inject, Pipe, type PipeTransform} from '@angular/core';

import {GlobalTransactionDto} from '../models/transaction/transaction.model';

type SignedAmountWithCurrency = Pick<
  GlobalTransactionDto,
  'amount' | 'transactionType' | 'currency'
>;

@Pipe({name: 'transactionAmount'})
export class TransactionAmountPipe implements PipeTransform {
  private readonly decimalPipe = inject(DecimalPipe);

  public transform<T extends SignedAmountWithCurrency>({
    transactionType,
    amount,
    currency,
  }: T): string {
    const sign = transactionType === 'credit' ? '+' : '-';
    const formatted = this.decimalPipe.transform(Math.abs(amount), '1.2-2') ?? '';
    return `${sign}${formatted} ${currency}`;
  }
}
