import {DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject} from '@angular/core';
import {Router} from '@angular/router';
import {
  AlertComponent,
  CardComponent,
  CmnCellDirective,
  CmnColumnComponent,
  DataTableComponent,
  DonutChartComponent,
  InstitutionAvatarComponent,
} from '@lifekit-hq/ui';

import {AppRoute} from '../../../../shared/enums/app-route/app-route.enum';
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
  private readonly router = inject(Router);
  public readonly store = inject(HoldingsStore);
  public readonly pnlPositiveClass = 'text-status-success';
  public readonly pnlNegativeClass = 'text-status-error';

  public navigateToDossier(symbol: string): void {
    void this.router.navigate([AppRoute.AssetDossier, symbol]);
  }
}
