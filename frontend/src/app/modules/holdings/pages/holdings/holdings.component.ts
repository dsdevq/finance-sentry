import {DecimalPipe} from '@angular/common';
import {ChangeDetectionStrategy, Component, inject, signal, ViewContainerRef} from '@angular/core';
import {
  AlertComponent,
  ButtonComponent,
  CardComponent,
  CmnCellDirective,
  CmnColumnComponent,
  CmnDialogService,
  DataTableComponent,
  DisclosureRowComponent,
  PageHeaderComponent,
  StatCardComponent,
  TagComponent,
} from '@dsdevq-common/ui';
import {take} from 'rxjs';

import {type Provider} from '../../../../shared/models/provider/provider.model';
import {SyncStatusLabelPipe} from '../../../../shared/pipes/sync-status-label.pipe';
import {SyncStatusVariantPipe} from '../../../../shared/pipes/sync-status-variant.pipe';
import {DisconnectDialogComponent} from '../../../bank-sync/components/disconnect-dialog/disconnect-dialog.component';
import {AccountsStore} from '../../../bank-sync/store/accounts/accounts.store';
import {CategoryLabelPipe} from '../../pipes/category-label.pipe';
import {CurrencyAmountPipe} from '../../pipes/currency-amount.pipe';
import {HoldingBalancePipe} from '../../pipes/holding-balance.pipe';
import {HoldingsStore} from '../../store/holdings.store';

const DISCONNECTABLE_PROVIDERS = new Set<Provider>(['binance', 'ibkr']);

const TAB_BASE_CLASSES = 'px-cmn-4 py-cmn-2 text-cmn-sm font-medium transition-colors border-b-2';
const TAB_ACTIVE_CLASSES = 'text-text-primary border-accent-default';
const TAB_INACTIVE_CLASSES = 'text-text-secondary border-transparent hover:text-text-primary';

export type HoldingsTab = 'holdings' | 'positions';

@Component({
  selector: 'fns-holdings',
  imports: [
    AlertComponent,
    ButtonComponent,
    CardComponent,
    CategoryLabelPipe,
    CmnCellDirective,
    CmnColumnComponent,
    CurrencyAmountPipe,
    DataTableComponent,
    DecimalPipe,
    DisclosureRowComponent,
    HoldingBalancePipe,
    PageHeaderComponent,
    StatCardComponent,
    SyncStatusLabelPipe,
    SyncStatusVariantPipe,
    TagComponent,
  ],
  templateUrl: './holdings.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AccountsStore, HoldingsStore],
})
export class HoldingsComponent {
  private readonly dialog = inject(CmnDialogService);
  private readonly viewContainerRef = inject(ViewContainerRef);

  private readonly accountsStore = inject(AccountsStore);

  public readonly store = inject(HoldingsStore);
  public readonly activeTab = signal<HoldingsTab>('holdings');
  public readonly pnlPositiveClass = 'text-status-success';
  public readonly pnlNegativeClass = 'text-status-error';

  public tabClass(tab: HoldingsTab): string {
    return `${TAB_BASE_CLASSES} ${this.activeTab() === tab ? TAB_ACTIVE_CLASSES : TAB_INACTIVE_CLASSES}`;
  }

  public switchTab(tab: HoldingsTab): void {
    this.activeTab.set(tab);
    if (tab === 'positions' && !this.store.positionsLoaded()) {
      this.store.loadPositions();
    }
  }

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
          this.accountsStore.disconnectBinance();
        } else if (provider === 'ibkr') {
          this.accountsStore.disconnectIBKR();
        }
      });
  }
}
