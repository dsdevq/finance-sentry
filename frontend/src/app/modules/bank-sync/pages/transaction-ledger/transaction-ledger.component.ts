import {DatePipe, DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  AlertComponent,
  BadgeComponent,
  ButtonComponent,
  CardComponent,
  StatCardComponent,
} from '@dsdevq-common/ui';

import {type GlobalTransactionDto} from '../../models/transaction.model';
import {TransactionLedgerStore} from '../../store/transaction-ledger/transaction-ledger.store';

const AMOUNT_DECIMAL_PLACES = 2;

@Component({
  selector: 'fns-transaction-ledger',
  standalone: true,
  imports: [
    AlertComponent,
    BadgeComponent,
    ButtonComponent,
    CardComponent,
    DatePipe,
    DecimalPipe,
    StatCardComponent,
  ],
  templateUrl: './transaction-ledger.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TransactionLedgerStore],
})
export class TransactionLedgerComponent {
  public readonly store = inject(TransactionLedgerStore);

  public formatAmount(t: GlobalTransactionDto): string {
    const sign = t.transactionType === 'credit' ? '+' : '-';
    return `${sign}${Math.abs(t.amount).toFixed(AMOUNT_DECIMAL_PLACES)}`;
  }

  public amountClass(t: GlobalTransactionDto): string {
    return t.transactionType === 'credit' ? 'text-status-success' : 'text-status-error';
  }
}
