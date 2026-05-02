import {DatePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Router} from '@angular/router';
import {
  AlertComponent,
  ButtonComponent,
  CardComponent,
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
    CmnCellDirective,
    CmnColumnComponent,
    DataTableComponent,
    DatePipe,
    FormsModule,
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
  public startDate = '';
  public endDate = '';

  public goBack(): void {
    void this.router.navigate(['/accounts']);
  }

  public applyFilters(): void {
    this.store.setDateRange(this.startDate, this.endDate);
    this.store.load();
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
