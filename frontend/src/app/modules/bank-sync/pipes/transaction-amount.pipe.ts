import {DecimalPipe} from '@angular/common';
import {inject, Pipe, type PipeTransform} from '@angular/core';

import {Transaction, type TransactionType} from '../models/transaction/transaction.model';

type SignedAmount = Pick<Transaction, 'amount' | 'transactionType'>;
@Pipe({name: 'transactionAmount'})
export class TransactionAmountPipe implements PipeTransform {
  private readonly decimalPipe = inject(DecimalPipe);

  public transform<T extends SignedAmount>({transactionType, amount}: T): string {
    const sign = transactionType === 'credit' ? '+' : '-';
    const formatted = this.decimalPipe.transform(Math.abs(amount)) ?? '';
    return `${sign}${formatted}`;
  }
}
