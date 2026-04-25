import {DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  AlertComponent,
  BadgeComponent,
  ButtonComponent,
  CardComponent,
  StatCardComponent,
} from '@dsdevq-common/ui';

import {SyncStatusLabelPipe} from '../../../../shared/pipes/sync-status-label.pipe';
import {SyncStatusVariantPipe} from '../../../../shared/pipes/sync-status-variant.pipe';
import {CategoryLabelPipe} from '../../pipes/category-label.pipe';
import {CurrencyAmountPipe} from '../../pipes/currency-amount.pipe';
import {HoldingBalancePipe} from '../../pipes/holding-balance.pipe';
import {HoldingsStore} from '../../store/holdings.store';

@Component({
  selector: 'fns-holdings',
  imports: [
    AlertComponent,
    BadgeComponent,
    ButtonComponent,
    CardComponent,
    CategoryLabelPipe,
    CurrencyAmountPipe,
    DecimalPipe,
    HoldingBalancePipe,
    StatCardComponent,
    SyncStatusLabelPipe,
    SyncStatusVariantPipe,
  ],
  templateUrl: './holdings.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [HoldingsStore],
})
export class HoldingsComponent {
  public readonly store = inject(HoldingsStore);
}
