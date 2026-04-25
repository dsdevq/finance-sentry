import {Pipe, type PipeTransform} from '@angular/core';

import {type GlobalTransactionDto} from '../models/transaction/transaction.model';

@Pipe({name: 'transactionAmountClass'})
export class TransactionAmountClassPipe implements PipeTransform {
  public transform(transaction: GlobalTransactionDto): string {
    return transaction.transactionType === 'credit' ? 'text-status-success' : 'text-status-error';
  }
}
