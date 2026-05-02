import {DatePipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  BadgeComponent,
  ButtonComponent,
  CardComponent,
  IconComponent,
  StatCardComponent,
} from '@dsdevq-common/ui';

import {AppCurrencyPipe} from '../../../../core/pipes/app-currency.pipe';
import {type SubscriptionSort} from '../../models/subscription/subscription.model';
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
    BadgeComponent,
    ButtonComponent,
    CardComponent,
    DatePipe,
    IconComponent,
    StatCardComponent,
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
}
