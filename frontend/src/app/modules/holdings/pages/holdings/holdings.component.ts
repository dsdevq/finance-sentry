import {DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject, ViewContainerRef} from '@angular/core';
import {
  AlertComponent,
  BadgeComponent,
  ButtonComponent,
  CardComponent,
  CmnDialogService,
  StatCardComponent,
} from '@dsdevq-common/ui';
import {take} from 'rxjs';

import {type Provider} from '../../../../shared/models/provider/provider.model';
import {SyncStatusLabelPipe} from '../../../../shared/pipes/sync-status-label.pipe';
import {SyncStatusVariantPipe} from '../../../../shared/pipes/sync-status-variant.pipe';
import {DisconnectDialogComponent} from '../../../bank-sync/components/disconnect-dialog/disconnect-dialog.component';
import {CategoryLabelPipe} from '../../pipes/category-label.pipe';
import {CurrencyAmountPipe} from '../../pipes/currency-amount.pipe';
import {HoldingBalancePipe} from '../../pipes/holding-balance.pipe';
import {HoldingsStore} from '../../store/holdings.store';

const DISCONNECTABLE_PROVIDERS = new Set<Provider>(['binance', 'ibkr']);

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
  private readonly dialog = inject(CmnDialogService);
  private readonly viewContainerRef = inject(ViewContainerRef);

  public readonly store = inject(HoldingsStore);

  public canDisconnect(provider: string): boolean {
    return DISCONNECTABLE_PROVIDERS.has(provider as Provider);
  }

  public disconnect(provider: string, displayName: string): void {
    if (!this.canDisconnect(provider)) {
      return;
    }
    const ref = this.dialog.open<boolean>(DisconnectDialogComponent, {
      title: `Disconnect ${displayName}`,
      size: 'sm',
      viewContainerRef: this.viewContainerRef,
      data: {providerName: displayName},
    });
    ref
      .afterClosed()
      .pipe(take(1))
      .subscribe(confirmed => {
        if (confirmed !== true) {
          return;
        }
        if (provider === 'binance') {
          this.store.disconnectBinance();
        } else if (provider === 'ibkr') {
          this.store.disconnectIBKR();
        }
      });
  }
}
