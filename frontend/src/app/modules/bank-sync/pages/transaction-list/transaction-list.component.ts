import {DatePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {Router} from '@angular/router';
import {
  AlertComponent,
  ButtonComponent,
  CardComponent,
  ChipComponent,
  CmnCellDirective,
  CmnColumnComponent,
  DataTableComponent,
} from '@dsdevq-common/ui';

import {MerchantCategoryPipe} from '../../../../shared/pipes/merchant-category.pipe';
import {TransactionAmountPipe} from '../../pipes/transaction-amount.pipe';
import {TransactionsStore} from '../../store/transactions/transactions.store';

@Component({
  selector: 'fns-transaction-list',
  imports: [
    AlertComponent,
    ButtonComponent,
    CardComponent,
    ChipComponent,
    CmnCellDirective,
    CmnColumnComponent,
    DataTableComponent,
    DatePipe,
    MerchantCategoryPipe,
    TransactionAmountPipe,
  ],
  templateUrl: './transaction-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TransactionsStore],
})
export class TransactionListComponent {
  private readonly router = inject(Router);

  public readonly store = inject(TransactionsStore);

  public goBack(): void {
    void this.router.navigate(['/accounts']);
  }

  public previousPage(): void {
    this.store.previousPage();
    this.store.load();
  }

  public nextPage(): void {
    this.store.nextPage();
    this.store.load();
  }

  public retry(): void {
    this.store.load();
  }
}
