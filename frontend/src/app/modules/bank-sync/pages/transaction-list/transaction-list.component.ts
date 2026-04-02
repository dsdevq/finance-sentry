import {Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {ActivatedRoute, Router} from '@angular/router';
import {BankSyncService} from '../../services/bank-sync.service';
import {Transaction} from '../../models/transaction.model';

const PAGE_SIZE = 50;

@Component({
  selector: 'app-transaction-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './transaction-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionListComponent implements OnInit {
  public accountId = '';
  public bankName = '';
  public currency = '';

  public transactions: Transaction[] = [];
  public isLoading = false;
  public errorMessage: string | null = null;

  public offset = 0;
  public totalCount = 0;
  public readonly pageSize = PAGE_SIZE;

  public startDate = '';
  public endDate = '';

  private readonly bankSyncService = inject(BankSyncService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  public get currentPage(): number {
    return Math.floor(this.offset / this.pageSize) + 1;
  }

  public get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  public get hasPrevious(): boolean {
    return this.offset > 0;
  }

  public get hasNext(): boolean {
    return this.offset + this.pageSize < this.totalCount;
  }

  public ngOnInit(): void {
    this.accountId = this.route.snapshot.paramMap.get('accountId') ?? '';
    this.loadTransactions();
  }

  public loadTransactions(): void {
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

  public applyFilters(): void {
    this.offset = 0;
    this.loadTransactions();
  }

  public previousPage(): void {
    if (this.hasPrevious) {
      this.offset = Math.max(0, this.offset - this.pageSize);
      this.loadTransactions();
    }
  }

  public nextPage(): void {
    if (this.hasNext) {
      this.offset += this.pageSize;
      this.loadTransactions();
    }
  }

  public goBack(): void {
    void this.router.navigate(['/accounts']);
  }
}
