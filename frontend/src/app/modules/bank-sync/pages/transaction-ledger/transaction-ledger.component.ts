import {DatePipe, DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, computed, inject} from '@angular/core';
import {toSignal} from '@angular/core/rxjs-interop';
import {ActivatedRoute, Router} from '@angular/router';
import {
  AsyncStateComponent,
  ButtonComponent,
  CardComponent,
  CmnDrawerService,
  IconComponent,
  StatCardComponent,
  TagComponent,
} from '@dsdevq-common/ui';
import {map} from 'rxjs';

import {MerchantCategoryPipe} from '../../../../shared/pipes/merchant-category.pipe';
import {MerchantCategoryUtils} from '../../../../shared/utils/merchant-category.utils';
import {TransactionDrawerComponent} from '../../components/transaction-drawer/transaction-drawer.component';
import {type GlobalTransactionDto} from '../../models/transaction/transaction.model';
import {TransactionAmountPipe} from '../../pipes/transaction-amount.pipe';
import {TransactionAmountClassPipe} from '../../pipes/transaction-amount-class.pipe';
import {TransactionLedgerStore} from '../../store/transaction-ledger/transaction-ledger.store';

@Component({
  selector: 'fns-transaction-ledger',
  imports: [
    AsyncStateComponent,
    TagComponent,
    ButtonComponent,
    CardComponent,
    DatePipe,
    DecimalPipe,
    IconComponent,
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
  private readonly drawer = inject(CmnDrawerService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  public readonly store = inject(TransactionLedgerStore);

  public readonly activeCategory = toSignal(
    this.route.queryParamMap.pipe(map(p => p.get('category'))),
    {initialValue: null}
  );

  public readonly activeCategoryLabel = computed(() => {
    const cat = this.activeCategory();
    return cat ? MerchantCategoryUtils.format(cat) : null;
  });

  public readonly displayedTransactions = computed(() => {
    const cat = this.activeCategory();
    const all = this.store.transactions();
    if (!cat) {
      return all;
    }
    const target = cat.toLowerCase();
    return all.filter(t => t.merchantCategory?.toLowerCase() === target);
  });

  public openDrawer(tx: GlobalTransactionDto): void {
    this.drawer.open(TransactionDrawerComponent, {
      title: tx.description,
      data: tx,
      width: '480px',
    });
  }

  public clearCategory(): void {
    void this.router.navigate([], {queryParams: {category: null}, queryParamsHandling: 'merge'});
  }
}
