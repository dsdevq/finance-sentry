import {DatePipe, SlicePipe, UpperCasePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {ButtonComponent, CardComponent, StatCardComponent} from '@dsdevq-common/ui';

import {AppCurrencyPipe} from '../../../../core/pipes/app-currency.pipe';
import {type SubscriptionSort} from '../../models/subscription/subscription.model';
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
    DatePipe,
    MerchantColorPipe,
    SlicePipe,
    StatCardComponent,
    UpperCasePipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [SubscriptionsStore],
  templateUrl: './subscriptions.component.html',
})
export class SubscriptionsComponent {
  public readonly store = inject(SubscriptionsStore);
  public readonly sortOptions = SORT_OPTIONS;

  public daysUntil(dateStr: string): number {
    return Math.ceil((new Date(dateStr).getTime() - Date.now()) / MS_PER_DAY);
  }

  public setSort(sort: SubscriptionSort): void {
    this.store.setSort(sort);
  }

  public confirmDismiss(): void {
    const id = this.store.dismissTargetId();
    if (id) {
      this.store.dismiss(id);
    }
  }

  public restore(id: string): void {
    this.store.restore(id);
  }
}
