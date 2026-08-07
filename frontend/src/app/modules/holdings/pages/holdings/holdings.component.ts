import {DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {
  AlertComponent,
  CardComponent,
  CmnCellDirective,
  CmnColumnComponent,
  DataTableComponent,
  DonutChartComponent,
  InstitutionAvatarComponent,
} from '@dsdevq-common/ui';

import {AssetLogoPipe} from '../../../../shared/pipes/asset-logo.pipe';
import {CurrencyAmountPipe} from '../../pipes/currency-amount.pipe';
import {HoldingsStore} from '../../store/holdings.store';

@Component({
  selector: 'fns-investments',
  imports: [
    AlertComponent,
    AssetLogoPipe,
    CardComponent,
    CmnCellDirective,
    CmnColumnComponent,
    CurrencyAmountPipe,
    DataTableComponent,
    DecimalPipe,
    DonutChartComponent,
    InstitutionAvatarComponent,
  ],
  templateUrl: './holdings.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [HoldingsStore],
})
export class InvestmentsComponent {
  public readonly store = inject(HoldingsStore);
  public readonly pnlPositiveClass = 'text-status-success';
  public readonly pnlNegativeClass = 'text-status-error';
}
