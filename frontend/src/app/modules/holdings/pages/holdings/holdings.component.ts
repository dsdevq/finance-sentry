import {DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  AlertComponent,
  BadgeComponent,
  ButtonComponent,
  CardComponent,
  StatCardComponent,
} from '@dsdevq-common/ui';

import {type AccountBalanceItem} from '../../../bank-sync/models/wealth.model';
import {HoldingsStore} from '../../store/holdings.store';

const BALANCE_DECIMAL_PLACES = 2;

function resolveCategoryLabel(category: string): string {
  if (category === 'Banking') {
    return 'Banking';
  }
  if (category === 'Brokerage') {
    return 'Brokerage & Investment';
  }
  if (category === 'Crypto') {
    return 'Digital Assets';
  }
  return category;
}

function resolveBadgeVariant(syncStatus: string): 'success' | 'warning' | 'error' {
  if (syncStatus === 'synced') {
    return 'success';
  }
  if (syncStatus === 'stale') {
    return 'warning';
  }
  return 'error';
}

@Component({
  selector: 'fns-holdings',
  standalone: true,
  imports: [
    AlertComponent,
    BadgeComponent,
    ButtonComponent,
    CardComponent,
    DecimalPipe,
    StatCardComponent,
  ],
  templateUrl: './holdings.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [HoldingsStore],
})
export class HoldingsComponent {
  public readonly store = inject(HoldingsStore);

  public getCategoryLabel(category: string): string {
    return resolveCategoryLabel(category);
  }

  public getBadgeVariant(account: AccountBalanceItem): 'success' | 'warning' | 'error' {
    return resolveBadgeVariant(account.syncStatus);
  }

  public formatBalance(account: AccountBalanceItem): string {
    const value = account.balanceInBaseCurrency ?? account.currentBalance;
    return value.toFixed(BALANCE_DECIMAL_PLACES);
  }
}
