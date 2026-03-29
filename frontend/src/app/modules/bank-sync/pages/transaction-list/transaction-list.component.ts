import {
  Component,
  OnInit,
  inject,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { BankSyncService } from '../../services/bank-sync.service';
import { Transaction } from '../../models/transaction.model';

const PAGE_SIZE = 50;

@Component({
  selector: 'app-transaction-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './transaction-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionListComponent implements OnInit {
  private readonly bankSyncService = inject(BankSyncService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  accountId = '';
  bankName = '';
  currency = '';

  transactions: Transaction[] = [];
  isLoading = false;
  errorMessage: string | null = null;

  offset = 0;
  totalCount = 0;
  readonly pageSize = PAGE_SIZE;

  startDate = '';
  endDate = '';

  get currentPage(): number {
    return Math.floor(this.offset / this.pageSize) + 1;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get hasPrevious(): boolean {
    return this.offset > 0;
  }

  get hasNext(): boolean {
    return this.offset + this.pageSize < this.totalCount;
  }

  ngOnInit(): void {
    this.accountId = this.route.snapshot.paramMap.get('accountId') ?? '';
    this.loadTransactions();
  }

  loadTransactions(): void {
    if (!this.accountId) return;

    this.isLoading = true;
    this.errorMessage = null;

    this.bankSyncService
      .getTransactions(this.accountId, {
        offset: this.offset,
        limit: this.pageSize,
        startDate: this.startDate || undefined,
        endDate: this.endDate || undefined,
        sort: 'date:desc',
      })
      .subscribe({
        next: (res) => {
          this.transactions = res.transactions;
          this.totalCount = res.pagination.totalCount;
          this.bankName = res.bankName;
          this.currency = res.currency;
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.errorMessage = 'Failed to load transactions. Please try again.';
          this.isLoading = false;
          this.cdr.markForCheck();
        },
      });
  }

  applyFilters(): void {
    this.offset = 0;
    this.loadTransactions();
  }

  previousPage(): void {
    if (this.hasPrevious) {
      this.offset = Math.max(0, this.offset - this.pageSize);
      this.loadTransactions();
    }
  }

  nextPage(): void {
    if (this.hasNext) {
      this.offset += this.pageSize;
      this.loadTransactions();
    }
  }

  goBack(): void {
    void this.router.navigate(['/accounts']);
  }
}
