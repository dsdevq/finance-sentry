import {DatePipe, SlicePipe, UpperCasePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject, ViewContainerRef} from '@angular/core';
import {
  ButtonComponent,
  CardComponent,
  ChipComponent,
  CmnDialogService,
  ConfirmDialogComponent,
  ListItemRowComponent,
  PageHeaderComponent,
  StatCardComponent,
} from '@dsdevq-common/ui';
import {take} from 'rxjs';

import {AppCurrencyPipe} from '../../../../core/pipes/app-currency.pipe';
import {
  type Subscription,
  type SubscriptionSort,
} from '../../models/subscription/subscription.model';
import {MerchantColorPipe} from '../../pipes/merchant-color.pipe';
import {SubscriptionsStore} from '../../store/subscriptions/subscriptions.store';

const MS_PER_DAY = 86_400_000;

const SORT_OPTIONS: {value: SubscriptionSort; label: string}[] = [
  {value: 'date', label: 'Next charge'},
  {value: 'amount', label: 'Amount'},
  {value: 'name', label: 'Name'},
];

@Component({
  selector: 'fns-subscriptions',
  imports: [
    AppCurrencyPipe,
    ButtonComponent,
    CardComponent,
    ChipComponent,
    DatePipe,
    ListItemRowComponent,
    MerchantColorPipe,
    PageHeaderComponent,
    SlicePipe,
    StatCardComponent,
    UpperCasePipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SubscriptionsStore],
  templateUrl: './subscriptions.component.html',
})
export class SubscriptionsComponent {
  private readonly dialog = inject(CmnDialogService);
  private readonly viewContainerRef = inject(ViewContainerRef);

  public readonly store = inject(SubscriptionsStore);
  public readonly sortOptions = SORT_OPTIONS;

  public daysUntil(dateStr: string): number {
    return Math.ceil((new Date(dateStr).getTime() - Date.now()) / MS_PER_DAY);
  }

  public setSort(sort: SubscriptionSort): void {
    this.store.setSort(sort);
  }

  public dismiss(sub: Pick<Subscription, 'id' | 'merchantName'>): void {
    const ref = this.dialog.open<boolean>(ConfirmDialogComponent, {
      data: {
        title: `Dismiss ${sub.merchantName}?`,
        message: `This will hide ${sub.merchantName} from your subscriptions. It will survive future detection runs.`,
        confirmLabel: 'Dismiss',
        cancelLabel: 'Keep',
        confirmVariant: 'destructive',
      },
      size: 'sm',
      viewContainerRef: this.viewContainerRef,
    });
    ref
      .afterClosed()
      .pipe(take(1))
      .subscribe(confirmed => {
        if (confirmed !== true) {
          return;
        }
        this.store.dismiss(sub.id);
      });
  }

  public restore(id: string): void {
    this.store.restore(id);
  }
}
