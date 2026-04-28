import {DatePipe, DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  AlertComponent,
  BadgeComponent,
  ButtonComponent,
  CardComponent,
  StatCardComponent,
} from '@dsdevq-common/ui';

import {MerchantCategoryPipe} from '../../../../shared/pipes/merchant-category.pipe';
import {TransactionAmountPipe} from '../../pipes/transaction-amount.pipe';
import {TransactionAmountClassPipe} from '../../pipes/transaction-amount-class.pipe';
import {TransactionLedgerStore} from '../../store/transaction-ledger/transaction-ledger.store';

@Component({
  selector: 'fns-transaction-ledger',
  imports: [
    AlertComponent,
    BadgeComponent,
    ButtonComponent,
    CardComponent,
    DatePipe,
    DecimalPipe,
    MerchantCategoryPipe,
    StatCardComponent,
    TransactionAmountClassPipe,
    TransactionAmountPipe,
  ],
  templateUrl: './transaction-ledger.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TransactionLedgerStore],
})
export class TransactionLedgerComponent {
  public readonly store = inject(TransactionLedgerStore);
}
