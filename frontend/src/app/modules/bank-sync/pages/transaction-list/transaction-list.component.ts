import {CommonModule} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject, OnInit} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute, Router} from '@angular/router';

import {TransactionsStore} from '../../store/transactions/transactions.store';

@Component({
  selector: 'fns-transaction-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './transaction-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [TransactionsStore],
})
export class TransactionListComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  public readonly store = inject(TransactionsStore);
  public startDate = '';
  public endDate = '';

  public ngOnInit(): void {
    const accountId = this.route.snapshot.paramMap.get('accountId') ?? '';
    this.store.setAccountId(accountId);
    this.store.load();
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

  public goBack(): void {
    void this.router.navigate(['/accounts']);
  }
}
