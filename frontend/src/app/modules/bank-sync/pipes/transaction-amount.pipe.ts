import {DecimalPipe} from '@angular/common';
import {inject, Pipe, type PipeTransform} from '@angular/core';

import {type GlobalTransactionDto} from '../models/transaction/transaction.model';

@Pipe({name: 'transactionAmount'})
export class TransactionAmountPipe implements PipeTransform {
  private readonly decimalPipe = inject(DecimalPipe);

  public transform({transactionType, amount}: GlobalTransactionDto): string {
    const sign = transactionType === 'credit' ? '+' : '-';
    const formatted = this.decimalPipe.transform(Math.abs(amount)) ?? '';
    return `${sign}${formatted}`;
  }
}
