import {DatePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Router} from '@angular/router';
import {
  AlertComponent,
  ButtonComponent,
  CardComponent,
  DataTableComponent,
  TableColumn,
} from '@dsdevq-common/ui';

import {MerchantCategoryUtils} from '../../../../shared/utils/merchant-category.utils';
import {type Transaction} from '../../models/transaction/transaction.model';
import {TransactionsStore} from '../../store/transactions/transactions.store';

const DATE_PIPE = new DatePipe('en-US');
const AMOUNT_DECIMALS = 2;

const TRANSACTION_COLUMNS: TableColumn<Transaction>[] = [
  {
    key: 'date',
    header: 'Date',
    cell: tx => DATE_PIPE.transform(tx.postedDate ?? tx.pendingDate, 'mediumDate') ?? '—',
  },
  {key: 'description', header: 'Description', cell: tx => tx.description},
  {
    key: 'amount',
    header: 'Amount',
    align: 'right',
    cell: tx => {
      const sign = tx.transactionType === 'credit' ? '+' : '-';
      return `${sign}${Math.abs(tx.amount).toFixed(AMOUNT_DECIMALS)}`;
    },
  },
  {key: 'type', header: 'Type', align: 'center', cell: tx => tx.transactionType ?? '—'},
  {
    key: 'category',
    header: 'Category',
    cell: tx => (tx.merchantCategory ? MerchantCategoryUtils.format(tx.merchantCategory) : '—'),
  },
  {
    key: 'status',
    header: 'Status',
    align: 'center',
    cell: tx => (tx.isPending ? 'Pending' : 'Posted'),
  },
];

@Component({
  selector: 'fns-transaction-list',
  imports: [AlertComponent, ButtonComponent, CardComponent, DataTableComponent, FormsModule],
  templateUrl: './transaction-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TransactionsStore],
})
export class TransactionListComponent {
  private readonly router = inject(Router);

  public readonly store = inject(TransactionsStore);
  public readonly columns = TRANSACTION_COLUMNS;
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
